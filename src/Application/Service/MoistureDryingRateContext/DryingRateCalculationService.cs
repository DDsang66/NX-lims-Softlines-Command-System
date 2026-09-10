namespace NX_lims_Softlines_Command_System.src.Application.Service.MoistureDryingRateContext;

/// <summary>
/// NF5022 测试方法 —— 对应原软件 comboBox2 的两种结束条件。
/// GBT2008=GBT 21655.1 2008（残留不足5mg，或相邻采样蒸发增量不足3mg，均需点数大于3）；
/// GBT2023=GBT 21655.1 2023（残留不超过20mg，或采样间隔(分)×(点数−1)达 60 即测满60分钟，均需点数大于3）。
/// 注意 60 是"分钟"不是秒（sp 单位即分钟，见 Nf5022Formulas.Time）。
/// </summary>
public enum Nf5022TestMethod
{
    Gbt2008 = 0,
    Gbt2023 = 1
}

/// <summary>NF5022 单工位输入：测试前称得的架重/干布重 + 测试期间按采样时序收到的原始重量(mg)。</summary>
public sealed record Nf5022StationInput(
    double FrameWeightMg,
    double ClothWeightMg,
    IReadOnlyList<double> RawWeightMg);

/// <summary>NF5022 单工位权威计算结果。未参与测试的工位 Participated=false，其余值均为默认 0。</summary>
public sealed record Nf5022StationResult(
    int Station,
    bool Participated,
    double WaterMg,
    double ClothWeightMg,
    int ResultPoint,
    double TimeMin,
    double RateMgPerHour,
    double SfclPermille,
    IReadOnlyList<double>? EvaporationCurveMg);

/// <summary>NF5022 整机权威计算结果。</summary>
public sealed record Nf5022CalculationResult(
    int SpaceTimeMin,
    int ResidualMinute,
    Nf5022TestMethod Method,
    IReadOnlyList<Nf5022StationResult> Stations);

/// <summary>
/// NF5022 权威计算服务 —— 精确复刻原软件 SplitData 终止判定 + SaveNF5021Result 公式（bug除外），
/// 干燥速率除外：用蒸发曲线对时间的回归斜率替代原中点公式（见 Nf5022Formulas）。
/// </summary>
public static class DryingRateCalculationService
{
    public static Nf5022CalculationResult Calculate(
        int spaceTimeMin,
        int residualMinute,
        Nf5022TestMethod method,
        IReadOnlyList<Nf5022StationInput> stations)
    {
        var results = new List<Nf5022StationResult>(stations.Count);
        for (int i = 0; i < stations.Count; i++)
            results.Add(CalculateStation(i + 1, spaceTimeMin, residualMinute, method, stations[i]));
        return new Nf5022CalculationResult(spaceTimeMin, residualMinute, method, results);
    }

    private static Nf5022StationResult CalculateStation(
        int station, int sp, int residualMinute, Nf5022TestMethod method, Nf5022StationInput input)
    // sp = 采样间隔(分钟), 设备回帧 @n; 与残留时刻同为分钟, 相除得曲线下标
    {
        // ① 未参与：无任何重量采样 → 不产生结果
        if (input.RawWeightMg.Count == 0)
            return new Nf5022StationResult(station, Participated: false, 0, 0, 0, 0, 0, 0, null);

        // ① 首点定死滴水量（确定性推导，等价原软件 L659：water = raw − frame − cloth）
        double water = input.RawWeightMg[0] - input.FrameWeightMg - input.ClothWeightMg;
        if (water <= 0)
            return new Nf5022StationResult(station, Participated: false, 0, 0, 0, 0, 0, 0, null);

        // 蒸发曲线 evap[k] = 第 k+1 点蒸发量（首点为 0，与原 curve_weight[0]=0 一致）
        int n = input.RawWeightMg.Count;
        var evap = new double[n];
        evap[0] = 0;
        for (int k = 1; k < n; k++)
            evap[k] = water - (input.RawWeightMg[k] - input.FrameWeightMg - input.ClothWeightMg);

        // 终止判定：原软件对每帧检查且要求 点数>3（即 point≥4）
        int resultPoint = 0;
        for (int p = 4; p <= n; p++)
        {
            int k = p - 1;
            bool fired = method == Nf5022TestMethod.Gbt2008
                ? Math.Abs(evap[k] - water) < 5.0 || Math.Abs(evap[k] - evap[k - 1]) < 3.0
                : Math.Abs(evap[k] - water) <= 20.0 || sp * (p - 1) >= 60;
            if (fired) { resultPoint = p; break; }
        }
        // 永不触发（原软件会永久等待→丢记录）→ 用末点兜底，确保产生结果
        if (resultPoint == 0) resultPoint = n;

        double time = Nf5022Formulas.Time(resultPoint, sp);
        // 决策B: 干燥速率 = 蒸发曲线回归斜率(mg/h)，替代原中点公式; 曲线退化(NaN)→0
        double slope = Nf5022Formulas.RegressionSlopeMgPerHour(evap, resultPoint, sp);
        double rate = double.IsNaN(slope) || double.IsInfinity(slope) ? 0 : slope;
        double sfcl = Nf5022Formulas.Sfcl(evap, water, sp, residualMinute);

        return new Nf5022StationResult(station, Participated: true, water, input.ClothWeightMg, resultPoint, time, rate, sfcl, evap);
    }
}
