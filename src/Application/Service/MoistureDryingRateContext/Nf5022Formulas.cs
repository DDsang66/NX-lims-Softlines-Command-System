namespace NX_lims_Softlines_Command_System.src.Application.Service.MoistureDryingRateContext;

/// <summary>
/// NF5022 水分干燥速率纯公式 —— 与反编译原软件 <c>SaveNF5021Result</c> 对应。
/// 终止判定/蒸发时间/残留率逐条复刻原软件；【干燥速率】按决策改为蒸发曲线对时间的
/// 最小二乘回归斜率（替代原中点公式, 对拍单测不再逐条等于 mdb 存储值, 见 Rate 注释）。
/// 全部为静态纯函数，便于对拍/边界单测。
/// </summary>
public static class Nf5022Formulas
{
    /// <summary>
    /// 蒸发时间(min) = 采样间隔(sp, 分钟) × (终止点数 − 1)。
    /// 对应原公式 <c>zf_time = space_time * (result_point - 1)</c>。
    /// 单位自洽: sp=分钟 → time=分钟（GB/T 21655.1 自然蒸发, 对拍实测 time×rate≈water 验证）。
    /// </summary>
    public static double Time(int resultPoint, int spaceTime)
        => spaceTime * (resultPoint - 1);

    /// <summary>
    /// 干燥速率(mg/h) = 蒸发曲线对时间的【最小二乘回归斜率】（决策B: 全链路改用线性回归）。
    /// 拟合域取【干燥段】——首点到终止点共 resultPoint 个采样点（k=0..resultPoint−1），
    /// 与原中点公式只看 result_point/2 处同域; 若把终止后平台/尾部也纳入拟合, 斜率会被拉低。
    /// 单位自洽（与原公式一致, 决策7 存 mg/h）: x_k = 采样间隔(sp, 分) × k ÷ 60（小时）,
    /// y_k = evap[k]（mg）→ 回归斜率即 mg/h; slope = (n·Σxy − Σx·Σy) ÷ (n·Σxx − (Σx)²)。
    /// 替代原公式 <c>zf_rate = curve_weight[i, result_point/2] × 60 ÷ (space_time × (result_point/2))</c>：
    /// 中点公式只取单点蒸发量, 对曲线形状（斜坡缓急/尾部平台）不敏感且受末点抖动影响;
    /// 回归对整条干燥段最小二乘拟合, 数值与原 mdb 存储值不再逐条相等（决策: 接受差异）。
    /// 采样点不足 2 个 / 间隔非正 / 拟合退化（分母≈0）→ NaN（调用方负责转 0）。
    /// </summary>
    public static double RegressionSlopeMgPerHour(IReadOnlyList<double>? evapCurveMg, int resultPoint, int spaceTime)
    {
        if (evapCurveMg == null || resultPoint < 2 || spaceTime <= 0) return double.NaN;
        int n = Math.Min(resultPoint, evapCurveMg.Count);
        if (n < 2) return double.NaN;

        double sx = 0, sy = 0, sxx = 0, sxy = 0;
        for (int k = 0; k < n; k++)
        {
            double x = spaceTime * k / 60.0; // 小时
            double y = evapCurveMg[k];        // mg（单位自洽: 斜率 = mg/h）
            sx += x; sy += y; sxx += x * x; sxy += x * y;
        }
        double denom = n * sxx - sx * sx;
        if (Math.Abs(denom) < 1e-12) return double.NaN;
        return (n * sxy - sx * sy) / denom;
    }

    /// <summary>
    /// 水分残留率(‰) = 1000 − 指定时刻蒸发量 × 1000 ÷ 滴水量。
    /// 对应原公式 <c>sfcl[i] = 1000 - curve_weight[i, textBox11/space_time] * 1000 / water_weight[i]</c>。
    /// 下标 idx = 残留时刻(min) ÷ sp(min) —— 二者同为分钟才相除得曲线点序号（sp=秒则 idx 越界, 佐证 sp=分钟）。
    /// 滴水量=0 → 0（原软件守卫，避免除零）；残留时刻下标越界 → 1000（未记录点视蒸发=0，即"全未蒸发/全湿"——公式按曲线的结果，不代表真实干度）。
    /// </summary>
    public static double Sfcl(IReadOnlyList<double> evapCurveMg, double waterMg, int spaceTime, int residualMinute)
    {
        if (waterMg == 0) return 0;
        int idx = residualMinute / spaceTime;
        if (idx < 0 || idx >= evapCurveMg.Count) return 1000;
        return 1000.0 - evapCurveMg[idx] * 1000.0 / waterMg;
    }

    /// <summary>
    /// 指定时刻(min)的蒸发量(mg) —— GB21655 报告按模板 0/3/6..60min 网格填 Δmi/mi 的取值器。
    /// 曲线点序: evap[k] = 第 k+1 点（k×sp 分钟, 首点 0）。非整点时刻线性插值;
    /// 时刻超出已记录曲线（idx &gt; 末点）→ null, 引擎对超出记录范围的网格格留空（不凭空填数）;
    /// 时刻落在记录起点前（&lt;0 / idx≤0）→ 0。
    /// </summary>
    public static double? EvapAtMin(double minutes, IReadOnlyList<double>? evapCurveMg, int spaceTime)
    {
        if (evapCurveMg == null || evapCurveMg.Count == 0 || spaceTime <= 0) return null;
        double idx = minutes / spaceTime;
        if (idx <= 0) return 0.0;
        if (idx >= evapCurveMg.Count - 1)
            return idx > evapCurveMg.Count - 1 ? null : evapCurveMg[^1];
        int i0 = (int)Math.Floor(idx);
        int i1 = i0 + 1;
        double f = idx - i0;
        return evapCurveMg[i0] * (1 - f) + evapCurveMg[i1] * f;
    }
}
