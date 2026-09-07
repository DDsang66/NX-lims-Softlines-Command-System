namespace NX_lims_Softlines_Command_System.src.Application.Service.MoistureDryingRateContext;

/// <summary>
/// AATCC 201 算法/校准参数 —— 由 aatcc201_config 表整行映射而来（201config + 201testerconfig 合并）。
/// 温度偏置(temp_hw*)单位为 0.01℃（表里存的就是这个单位；原软件读库后 ÷100 显示、在接收端 ×1 加回原始 0.01 值）。
/// </summary>
public sealed record Aatcc201CalibrationParams(
    int SlopePoint,             // slope_point            —— 斜坡段斜率窗口（种子 20）
    int FlatPoint,              // flat_point             —— 平缓段/平台斜率窗口（种子 25）
    int SlopeDgNo,              // slope_dg_no            —— 斜率计算起始点数（种子 160）
    int SlopeContinueNo,        // slope_continue_no      —— 平台持续点数（种子 80）
    double SlopeContinueTemp,   // slope_continue_temp    —— 平台温差阈值 ℃（种子 3）
    double TempHw1,             // temp_hw1 —— 工位1 表面温度偏置（0.01℃）
    double TempHw2);            // temp_hw2 —— 工位2 表面温度偏置（0.01℃）

/// <summary>
/// AATCC 201 单帧原始遥测 —— 直接来自 AB BA 帧解码，未经偏置、未经抗抖动。
/// 偏置与抗抖动由计算服务内部复刻原软件接收逻辑执行，前端只需原样转发帧字节解码值。
/// </summary>
public sealed record Aatcc201FrameSample(
    int SurfaceRaw01,             // 表面温度原始值（0.01℃），帧字节对大端拼值
    int BoardRaw01,               // 加热板温度原始值（0.01℃），计算不用，随帧保留（报告曲线可用）
    int CoverStatus,              // 盖板状态 0=开 1=闭 —— 起点的唯一判据
    double FrameTimeSec);         // 本帧真实到达秒（保留给步骤5帧率校准；当前公式仍按"每点=1秒"，未使用）

/// <summary>AATCC 201 单工位输入。</summary>
public sealed record Aatcc201StationInput(
    double WaterMl,                             // 滴水量 mL（原 textBox8/5，默认 0.2）
    IReadOnlyList<Aatcc201FrameSample> Frames); // 原始帧时序（按接收顺序）

/// <summary>
/// AATCC 201 采纳温度点 —— 偏置+抗抖动后的表面温度（报告曲线图数据源）。
/// 点序 = 采样序（第 k 点 = 第 k 帧），与结果的 StartPoint/EndPoint 同坐标系。
/// </summary>
public sealed record Aatcc201TempPoint(
    double FrameTimeSec,    // 本帧真实到达秒（测试开始起；步骤5帧率校准后作曲线横轴/换算时间用）
    double SurfaceTemp01);  // 采纳后表面温度（0.01℃ 单位整数，已叠 temp_hw 偏置）

/// <summary>
/// AATCC 201 单工位权威计算结果。
/// Participated=false 表示未完成整轮测试（无帧 / 盖板无"闭→开"沿 / 未达平台 / 交点无效），其余值均为默认 0。
/// </summary>
public sealed record Aatcc201StationResult(
    int Station,
    bool Participated,      // 是否完成整轮测试（起点已记 + 平台确认 + 交点已求）
    double RateMgPerHour,   // 干燥速率 mg/h（整数）=(int)(水×1000÷(终点−起点)×3600+0.5)；报告 g/h=此值÷1000
    int StartPoint,         // 起点采样序号（盖板开沿，原 textBox2/9）
    int EndPoint,           // 终点采样序号（draw_two 交点，原 textBox10/12）
    int SlopeMaxPoint,      // return_slope_max_point 结果（斜坡线最大斜率处）
    int FlatMinPoint,       // return_flat_min_point 结果（平缓线最小斜率处）
    double DryingTimeSec,   // 干燥时间 = 终点 − 起点（每个采样点按 1 秒，原软件假设，见类注释）
    double WaterMl,         // 滴水量（原样回显）
    IReadOnlyList<Aatcc201TempPoint>? TempSeries = null); // 采纳后温度曲线（仅参与工位非空；报告嵌图数据源）

/// <summary>AATCC 201 整机权威计算结果（2 工位）。</summary>
public sealed record Aatcc201CalculationResult(
    IReadOnlyList<Aatcc201StationResult> Stations);

/// <summary>
/// AATCC 201 权威计算服务 —— 精确复刻原软件一套逻辑：
///   接收端偏置+抗抖动（MainForm.cs:2541-2566）
///   → data_show 状态机（MainForm.cs:2983-3147）：盖板"闭→开"沿起点、斜坡/平缓斜率
///   → slope_time_dg 斜坡→平台（MainForm.cs:2741-2762）
///   → flat_time_dg 平台自动结束（MainForm.cs:2764-2783）
///   → return_slope_max_point / return_flat_min_point（MainForm.cs:2785-2815）
///   → draw_two 精确交点（MainForm.cs:2817-2897）
///   → rate = (int)( water_ml×1000 ÷ (终点−起点) × 3600 + 0.5 ) mg/h（MainForm.cs:3044）
///     原软件再把该整数 ÷1000 显示成 g/h（mL/h）；本服务存 mg/h 原值，报告 g/h = ÷1000。
///
/// 移植原则（与 NF5022 一致）：数值/公式照原样，但不复刻原软件缺陷。
/// 与原软件的唯一差异：起点未记录（盖板无"闭→开"沿）或平台未确认时返回 Participated=false，
/// 而不是像原软件那样算出负速率/除零。
///
/// 时间单位说明（关键）：原软件速率公式把【每个采样点按 1 秒】计。
/// 而数据采样周期 = 200ms 定时器（MainForm.cs:2159），点间隔 = max(设备推送间隔, 200ms)。
/// 真机帧率未实测前保持照原样移植（方案 A）；每帧的 FrameTimeSec 真实时间戳随帧保留，
/// 待真机帧率校准后按用户拍板（A 照原样 / B 真实秒）微调速率与干燥时间。
/// </summary>
public static class Aatcc201CalculationService
{
    // ---------- 原软件硬编码阈值（MainForm.cs） ----------
    private const double JumpThreshold = 100.0;     // 抗抖动：|新值−上点|>1℃(100×0.01) 视为跳变
    private const int JumpPersistFrames = 4;        // 抗抖动：跳变需持续 >4 帧（第5帧起）才采纳
    private const double CoverTempTolerance = 20.0; // flat_time_dg/draw_two：温差容差 0.2℃(20×0.01)
    private const double SlopeLineCeiling = 3700.0; // draw_two：斜坡线延伸上限 37℃(0.01单位)，超限截断
    private const double StartSentinel = 1000.0;    // 起点未记录时的哨兵值（原 textBox 初始 "1000"）
    private const double RateSlopeOffset = 10.0;    // 速率斜率公式里的 −0.1℃ 常数（原软件如此，勿改）
    private const int FlatTimeWindow = 200;         // flat_time_dg：滑窗点数
    private const int FlatTimeAcceptCount = 150;    // flat_time_dg：窗内温差达标点数阈值
    private const int FlatTimeLead = 140;           // flat_time_dg：前置要求点数
    private const int AvgWindow = 15;               // draw_two：均值窗口点数
    private const double IntersectMinDist = 100.0;  // draw_two：交点搜索初始"最小间距"
    private const int IntersectBreakCount = 20;     // draw_two：交点搜索 break 计数

    /// <summary>对整机所有工位逐一计算（每工位独立状态机，互不影响）。</summary>
    public static Aatcc201CalculationResult Calculate(
        Aatcc201CalibrationParams calibration,
        IReadOnlyList<Aatcc201StationInput> stations)
    {
        var results = new List<Aatcc201StationResult>(stations.Count);
        for (int i = 0; i < stations.Count; i++)
            results.Add(CalculateStation(i, calibration, stations[i]));
        return new Aatcc201CalculationResult(results);
    }

    private static Aatcc201StationResult CalculateStation(
        int index, Aatcc201CalibrationParams cal, Aatcc201StationInput input)
    {
        int station = index + 1;

        // ① 无任何帧 → 未参与
        if (input.Frames.Count == 0)
            return NotParticipated(station, input.WaterMl);

        double tempBias01 = index == 0 ? cal.TempHw1 : cal.TempHw2; // 工位1/工位2 各自表面温度偏置

        // ② 复刻接收端：偏置 + 抗抖动，得到"采纳温度序列"（1-based，下标0空置）
        double[] temps = ApplyBiasAndAntiJitter(input.Frames, tempBias01);
        int n = temps.Length - 1; // 有效点数（1-based 最大下标）

        // ③ data_show 状态机（逐帧）
        int pt = 0;               // 当前点号（1-based，每帧 +1）
        int startTimeFlg = 0;     // "盖板闭合"见过标记
        int startPoint = -1;      // 起点（盖板开沿点号），-1=未记录
        int flatDgFlg = 0;        // 斜坡→平台 已判定标记
        int flatStartPoint = 0;   // 平台起始点号
        double[] rate7 = new double[n + 1];    // rate_7point[pt]：斜坡段斜率（窗口=slope_point）
        double[] rate25 = new double[n + 1];   // rate_25point[pt]：平缓段斜率（窗口=flat_point）
        int finishPt = -1;        // flat_time_dg 确认时的点号（-1=未自动结束）

        for (int k = 0; k < n; k++)
        {
            pt = k + 1;
            var frame = input.Frames[k];

            // ---- 起点：盖板"闭→开"沿（MainForm.cs:3002-3008） ----
            if (frame.CoverStatus == 1 && startPoint == -1)
                startTimeFlg = 1;                          // 先见过"闭合"才武装
            if (frame.CoverStatus == 0 && startPoint == -1 && startTimeFlg == 1)
                startPoint = pt;                           // 开盖瞬间记起点

            // ---- 速率斜率（MainForm.cs:3011-3017）----
            // 需起点之后 slope_dg_no 点才开始算；起点未记录时用哨兵 1000（等同原 textBox 初始值）
            double startValue = startPoint < 0 ? StartSentinel : startPoint;
            if (pt > cal.SlopeDgNo + startValue)
            {
                rate7[pt] = (temps[pt] - temps[pt - cal.SlopePoint] - RateSlopeOffset) / cal.SlopePoint;
                rate25[pt] = (temps[pt] - temps[pt - cal.FlatPoint] - RateSlopeOffset) / cal.FlatPoint;

                // ---- 斜坡→平台（MainForm.cs:3019-3024）----
                if (pt > cal.SlopeDgNo + startValue + cal.SlopeContinueNo && flatDgFlg == 0)
                {
                    if (SlopeTimeDg(cal, temps, rate7, pt, startValue) == 1)
                    {
                        flatDgFlg = 1;
                        flatStartPoint = pt - cal.SlopeContinueNo;
                    }
                }

                // ---- 平台确认 → 自动结束（MainForm.cs:3026-3044）----
                if (flatDgFlg == 1 && FlatTimeDg(cal, temps, pt, flatStartPoint) == 1)
                {
                    finishPt = pt;
                    break; // 原软件 test_flg=0，后续帧不再进状态机；结果用当前数据
                }
            }
        }

        // ④ 结果判定：起点未记录 / 未达平台 → 无结果
        if (startPoint < 0 || finishPt < 0)
            return NotParticipated(station, input.WaterMl);

        int slopeMax = ReturnSlopeMaxPoint(rate7, flatStartPoint, finishPt);
        int flatMin = ReturnFlatMinPoint(rate25, slopeMax, finishPt);
        int endPoint = DrawTwo(cal, temps, finishPt, slopeMax, flatMin);

        if (endPoint <= startPoint) // 交点无效（原软件会算负速率/除零 → 不复刻）
            return NotParticipated(station, input.WaterMl);

        double rate = RoundMgPerHour(input.WaterMl * 1000.0 / (endPoint - startPoint) * 3600.0);
        return new Aatcc201StationResult(
            station, Participated: true, rate,
            startPoint, endPoint, slopeMax, flatMin,
            DryingTimeSec: endPoint - startPoint, input.WaterMl,
            TempSeries: BuildTempSeries(input.Frames, temps));
    }

    private static Aatcc201StationResult NotParticipated(int station, double waterMl)
        => new Aatcc201StationResult(station, false, 0, 0, 0, 0, 0, 0, waterMl);

    /// <summary>
    /// 采纳后温度序列（偏置+抗抖动结果 temps[1..n]，下标 0 占位跳过）→ (帧真实秒, 温度 0.01℃) 点列表。
    /// 第 k 点对应第 k 帧（k=1..n），FrameTimeSec 取该帧到达秒，点序与 StartPoint/EndPoint 同坐标系。
    /// 仅参与工位调用；未参与工位 TempSeries 保持 null。
    /// </summary>
    private static List<Aatcc201TempPoint> BuildTempSeries(
        IReadOnlyList<Aatcc201FrameSample> frames, double[] temps)
    {
        var series = new List<Aatcc201TempPoint>(temps.Length - 1);
        for (int k = 1; k < temps.Length; k++)
            series.Add(new Aatcc201TempPoint(frames[k - 1].FrameTimeSec, temps[k]));
        return series;
    }

    // ============ 抗抖动（MainForm.cs:2541-2566） ============

    /// <summary>
    /// 复刻接收端：每帧 = 偏置(加 temp_hw) → 抗抖动(连续 >4 帧大跳变才采纳)。
    /// 返回 1-based 温度数组 temps[1..n]（下标0=0 空置）。首帧直接采纳（相当于连续第二次测试的稳态语义）。
    /// </summary>
    private static double[] ApplyBiasAndAntiJitter(IReadOnlyList<Aatcc201FrameSample> frames, double bias01)
    {
        var temps = new double[frames.Count + 1];
        double lastAccepted = 0;
        int jitterFlg = 0;
        for (int k = 0; k < frames.Count; k++)
        {
            double corrected = frames[k].SurfaceRaw01 + bias01; // 偏置叠加（0.01℃ 单位）
            if (Math.Abs(corrected - lastAccepted) > JumpThreshold)
                jitterFlg++;
            else
                jitterFlg = 0;

            if (jitterFlg == 0 || jitterFlg > JumpPersistFrames)
            {
                lastAccepted = corrected;
                jitterFlg = 0;
            }
            if (k == 0)
                lastAccepted = corrected; // 首帧直接采纳（稳态续测语义，见类注释）

            temps[k + 1] = lastAccepted;
        }
        return temps;
    }

    // ============ 平台检测（MainForm.cs:2741-2783） ============

    /// <summary>
    /// slope_time_dg —— 斜坡是否结束、进入平缓/平台段（MainForm.cs:2741-2762）。
    /// 满足其一即判"回升已展开"：① 近 slope_continue_no 点温差 &gt; slope_continue_temp℃；
    /// ② 近 slope_continue_no 点中 rate_7point&gt;0 的占比 ≥ 9/10（≈连续回升 80 点）。
    /// </summary>
    private static int SlopeTimeDg(
        Aatcc201CalibrationParams cal, double[] temps, double[] rate7, int pt, double startValue)
    {
        // 条件①：温度真实回升（0.01 单位比较，slope_continue_temp 存的是 ℃，×100 换算）
        if (pt > cal.SlopeDgNo + startValue + cal.SlopeContinueNo
            && temps[pt] - temps[pt - cal.SlopeContinueNo] > cal.SlopeContinueTemp * 100.0)
            return 1;

        // 条件②：最近 slope_continue_no 个点中斜坡斜率&gt;0 的占比（未计算处 rate7=0，不计入）
        int positiveCount = 0;
        for (int i = 0; i < cal.SlopeContinueNo; i++)
        {
            if (pt - i >= 1 && rate7[pt - i] > 0.0)
                positiveCount++;
        }
        return positiveCount > cal.SlopeContinueNo * 9 / 10 ? 1 : 0;
    }

    /// <summary>
    /// flat_time_dg —— 温度平台确认、自动结束（MainForm.cs:2764-2783）。
    /// 前置：点号 ≥ flat_start_point + slope_continue_no + 140；
    /// 判定：200 点滑窗内 |T[pt−i] − T[pt−flat_point−i]| &lt; 0.2℃ 的对数 &gt; 150。
    /// </summary>
    private static int FlatTimeDg(
        Aatcc201CalibrationParams cal, double[] temps, int pt, int flatStartPoint)
    {
        if (flatStartPoint + cal.SlopeContinueNo + FlatTimeLead > pt)
            return 0;

        int num = 0;
        for (int i = 0; i < FlatTimeWindow; i++)
        {
            if (pt - cal.FlatPoint - i > 0
                && Math.Abs(temps[pt - i] - temps[pt - cal.FlatPoint - i]) < CoverTempTolerance)
                num++;
        }
        return num > FlatTimeAcceptCount ? 1 : 0;
    }

    // ============ 特征点（MainForm.cs:2785-2815） ============

    /// <summary>return_slope_max_point —— [flat_start_point, finishPt) 内 rate_7point 最大处（回升最陡）。</summary>
    private static int ReturnSlopeMaxPoint(double[] rate7, int flatStartPoint, int finishPt)
    {
        int result = 0;
        double max = 0.0;
        for (int i = flatStartPoint; i < finishPt; i++)
        {
            if (max < rate7[i])
            {
                max = rate7[i];
                result = i;
            }
        }
        return result;
    }

    /// <summary>return_flat_min_point —— [slopeMax, finishPt−19) 内 rate_25point 最小处（最平缓）。</summary>
    private static int ReturnFlatMinPoint(double[] rate25, int slopeMax, int finishPt)
    {
        int result = 0;
        double min = 100000.0;
        for (int i = slopeMax; i < finishPt - 19; i++)
        {
            if (min >= rate25[i])
            {
                min = rate25[i];
                result = i;
            }
        }
        return result;
    }

    // ============ 精确交点 draw_two（MainForm.cs:2817-2897） ============

    /// <summary>
    /// draw_two —— 斜坡延长线与平缓线交点搜索，返回终点点号（原 textBox10/12）。
    /// 斜坡线：slope_max 附近两段 15 点均值连线，以斜率 (a1−a0)/slope_point 向前延伸（超 37℃ 截断）；
    /// 平缓线：flat_min 附近两段 15 点均值连线，以斜率 (b1−b0)/flat_point 向后延伸（不高于起点）；
    /// 交点：slope_max+1 .. flat_min−flat_point−2 范围内 |平缓−斜坡| &lt; 0.2℃ 且间距最小处。
    /// </summary>
    private static int DrawTwo(
        Aatcc201CalibrationParams cal, double[] temps, int finishPt, int slopeMax, int flatMin)
    {
        // ---- 斜坡线 ----
        double a0 = Avg15(temps, slopeMax - cal.SlopePoint - 7);
        double a1 = Avg15(temps, slopeMax - 7);
        var slopeDraw = new double[finishPt + 1];
        slopeDraw[slopeMax] = a1;
        for (int i = slopeMax + 1; i < finishPt; i++)
        {
            slopeDraw[i] = slopeDraw[i - 1] + (a1 - a0) / cal.SlopePoint;
            if (slopeDraw[i] > SlopeLineCeiling)
                break; // 超温截断（原软件如此，后续点保持 0）
        }

        // ---- 平缓线 ----
        double b0 = Avg15(temps, flatMin - cal.FlatPoint - 7);
        double b1 = Avg15(temps, flatMin - 7);
        if (b0 > b1)
            b1 = b0; // 原软件强制平缓线不高于起点
        var flatDraw = new double[finishPt + 1];
        flatDraw[flatMin - cal.FlatPoint] = b0;
        for (int i = flatMin - cal.FlatPoint - 1; i > 0; i--)
            flatDraw[i] = flatDraw[i + 1] - (b1 - b0) / cal.FlatPoint;

        // ---- 交点搜索 ----
        double minDist = IntersectMinDist;
        int endPoint = 0;
        int num = 0;
        for (int i = slopeMax + 1; i < flatMin - cal.FlatPoint - 1; i++)
        {
            double d = Math.Abs(flatDraw[i] - slopeDraw[i]);
            if (d < CoverTempTolerance && minDist >= d)
            {
                minDist = d;
                endPoint = i;
                num++;
            }
            if (num > 0)
                num++; // 原软件的双计数（命中后每点再 +1）
            if (num > IntersectBreakCount)
                break;
        }
        return endPoint;
    }

    /// <summary>15 点窗口平均：temps[base .. base+14]。越界按可用范围裁剪（原软件此处会越界崩溃，不做复刻）。</summary>
    private static double Avg15(double[] temps, int baseIndex)
    {
        int n = temps.Length - 1;
        int start = Math.Max(1, baseIndex);
        int end = Math.Min(n, baseIndex + AvgWindow - 1);
        if (end < start)
            return temps[1]; // 极端短序列兜底
        double sum = 0;
        for (int i = start; i <= end; i++)
            sum += temps[i];
        return sum / (end - start + 1);
    }

    /// <summary>
    /// 速率整数舍入 —— 原软件对 mg/h 值先 (int)(x+0.5) 取整（MainForm.cs:3044），再 ÷1000 显示成 g/h。
    /// 本服务存取整后的 mg/h 原值（整数），报告 g/h = 此值÷1000，与原软件显示值一致。
    /// </summary>
    private static double RoundMgPerHour(double x) => (int)(x + 0.5);
}
