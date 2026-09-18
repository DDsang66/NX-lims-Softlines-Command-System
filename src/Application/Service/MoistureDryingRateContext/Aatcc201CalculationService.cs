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
    int CoverStatus,              // 盖板状态 0=闭 1=开 —— 起点的唯一判据
    double FrameTimeSec);         // 本帧真实到达秒（帧率校准已于 2026-09-18 完成：真机 1 点 ≈ 1 秒，见类注释；本字段仍随帧保留，但公式与曲线横轴都不用）

/// <summary>
/// AATCC 201 单次测试输入。
/// Station = 本次测试实际使用的物理工位 1|2（测试3 复用工位时可能与先前重复）。
/// 0 = 未指定，由计算服务按槽位下标回退(index+1)。测试3 若重测工位1(index 2)但没带 Station，
/// 偏置会错拿 TempHw2 —— 前端送算必须显式带 Station。
/// </summary>
public sealed record Aatcc201StationInput(
    int Station,                                // 物理工位 1|2（0=按槽位下标回退）
    double WaterMl,                             // 滴水量 mL（原 textBox8/5，默认 0.2）
    IReadOnlyList<Aatcc201FrameSample> Frames); // 原始帧时序（按接收顺序）

/// <summary>
/// AATCC 201 采纳温度点 —— 偏置+抗抖动后的表面温度（报告曲线图数据源）。
/// 点序 = 采样序（第 k 点 = 第 k 帧），与结果的 StartPoint/EndPoint 同坐标系；报告曲线图横轴即点序。
/// </summary>
public sealed record Aatcc201TempPoint(
    double FrameTimeSec,    // 本帧真实到达秒（测试开始起；帧率校准已完成, 不作横轴, 见类注释）
    double SurfaceTemp01);  // 采纳后表面温度（0.01℃ 单位整数，已叠 temp_hw 偏置）

/// <summary>
/// AATCC 201 单次测试权威计算结果。
/// Participated=false 表示未完成整轮测试（无帧 / 盖板无"开→闭"沿 / 未达平台 / 终点无效），其余值均为默认 0。
/// Station = 本次测试实际使用的物理工位 1|2（测试3 复用工位时, 工位号可能重复出现）。
/// </summary>
public sealed record Aatcc201StationResult(
    int Station,
    bool Participated,      // 是否完成整轮测试（起点已记 + 平台确认 + 终点已求）
    double RateMgPerHour,   // 干燥速率 mg/h（整数）=(int)(水×1000÷(终点−起点)×3600+0.5)；报告 g/h=此值÷1000
    int StartPoint,         // 起点采样序号（盖板 1→0 沿，原 textBox2/9）
    int EndPoint,           // 终点采样序号（draw_two 两线收敛点，原 textBox10/12；口径含早停，见 DrawTwo）
    int SlopeMaxPoint,      // return_slope_max_point 结果（斜坡线最大斜率处）
    int FlatMinPoint,       // return_flat_min_point 结果（平缓线最小斜率处）
    double DryingTimeSec,   // 干燥时间 = 终点 − 起点（每个采样点按 1 秒，原软件假设，见类注释）
    double WaterMl,         // 滴水量（原样回显）
    IReadOnlyList<Aatcc201TempPoint>? TempSeries = null); // 采纳后温度曲线（仅参与工位非空；报告嵌图数据源）

/// <summary>
/// AATCC 201 整机权威计算结果（1..3 次测试, 顺序 = 报告槽位; 每次测试一个物理工位）。
/// </summary>
public sealed record Aatcc201CalculationResult(
    IReadOnlyList<Aatcc201StationResult> Stations);

/// <summary>
/// AATCC 201 报告曲线图上叠的两条 draw_two 辅助线 —— 横轴 = 点号，纵轴 = 0.01℃（与结果表同坐标系）。
/// 由 BuildDrawLines 从采纳温度序列重建，与 DrawTwo 共用同一份构造 —— 图上画的线就是算终点用的那两条线。
/// 但两条线的几何交点并不等于结果表的 EndPoint：DrawTwo 是"间距首次收进 0.2℃ 带内、再走 10 个点即早停"
/// 的扫描口径（照原软件逐行复刻），天生比几何交点早几个点（本仓库金标数据差 3 点，偏多少随带宽变化）。
/// 所以图上那条终点竖线画在 EndPoint 上、与几何交点有肉眼可辨的错位 —— 这是口径决定的，不是作图错位。
/// 两条线按构造都是直线（逐点累加同一个增量），故每条只需两个端点；裁到绘图窗口由绘图层做。
/// </summary>
public sealed record Aatcc201DrawLines(
    (double X, double Y) SlopeA,    // 斜坡线左端：点号 slopeMax−slope_point，纵轴取该处窗口均值
    (double X, double Y) SlopeB,    // 斜坡线右端：向前延伸到超 37℃ 截断处（或序列末尾）
    (double X, double Y) FlatA,     // 平缓线左端：向左延伸到 1 号点
    (double X, double Y) FlatB);    // 平缓线右端：点号 flatMin，纵轴取该处窗口均值（不高于左锚点）

/// <summary>
/// AATCC 201 权威计算服务 —— 精确复刻原软件一套逻辑：
///   接收端偏置+抗抖动（MainForm.cs:2541-2566）
///   → data_show 状态机（MainForm.cs:2983-3147）：盖板"开→闭"沿起点、斜坡/平缓斜率
///   → slope_time_dg 斜坡→平台（MainForm.cs:2741-2762）
///   → flat_time_dg 平台自动结束（MainForm.cs:2764-2783）
///   → return_slope_max_point / return_flat_min_point（MainForm.cs:2785-2815）
///   → draw_two 两线收敛点 = 终点（MainForm.cs:2817-2897）
///   → rate = (int)( water_ml×1000 ÷ (终点−起点) × 3600 + 0.5 ) mg/h（MainForm.cs:3044）
///     原软件再把该整数 ÷1000 显示成 g/h（mL/h）；本服务存 mg/h 原值，报告 g/h = ÷1000。
///
/// 移植原则（与 NF5022 一致）：数值/公式照原样，但不复刻原软件缺陷。
/// 与原软件的唯一差异：起点未记录（盖板无"开→闭"沿）或平台未确认时返回 Participated=false，
/// 而不是像原软件那样算出负速率/除零。
///
/// 时间单位说明（2026-09-18 真机实测后定案）：原软件速率公式把【每个采样点按 1 秒】计。
/// 该假设在真机上【成立】：2026-09-18 连 AATCC 真机实测帧间隔中位 993ms、P95 1009ms（1.01Hz），
/// 即 1 点 ≈ 1 秒，
/// 报告里 Start/End time (s) 印的点号单位是真的，速率与干燥时间【维持照原样移植】，
/// 不按 FrameTimeSec 换算（方案 A 定案，方案 B「真实秒」作废）。
///
/// 采点节拍说明：原软件记点走它自己的 200ms 定时器（MainForm.cs:2159，一个 tick 只记一点），
/// 设备推送间隔大于 200ms 时它不丢帧，两边点数一致 —— 真机 993ms 属于此档，
/// 所以本服务【每帧一点】的节拍与原软件等价，无需在前端按 200ms 抽稀。
/// 若将来换设备/固件把推送周期压到 200ms 以下，原软件会丢帧而本服务不会，
/// 那时点数与速率会分叉，需在前端按 200ms 抽稀后再记点。
///
/// 曲线横轴说明：报告曲线图横轴 = 【采样点号】, 与原软件屏幕图一致（MainForm.cs:3065/3162），
/// 不用 FrameTimeSec —— 前端采集非均匀（浏览器后台标签页定时器被节流到 1Hz），
/// 按墙钟秒画会把同一段曲线拉成不同形状；点号则与结果表的起点/终点严格同坐标系。
/// </summary>
public static class Aatcc201CalculationService
{
    // ---------- 原软件硬编码阈值（MainForm.cs） ----------
    private const double JumpThreshold = 100.0;     // 抗抖动：|新值−上点|>1℃(100×0.01) 视为跳变
    private const int JumpPersistFrames = 4;        // 抗抖动：跳变需持续 >4 帧（第5帧起）才采纳
    private const double CoverTempTolerance = 20.0; // flat_time_dg/draw_two：温差容差 0.2℃(20×0.01)
    /// <summary>draw_two：斜坡线延伸上限 37℃（0.01℃ 单位），超限截断（原软件如此）。报告作图也用这个上界。</summary>
    public const double SlopeLineCeiling01 = 3700.0;
    private const double StartSentinel = 1000.0;    // 起点未记录时的哨兵值（原 textBox 初始 "1000"）
    private const double RateSlopeOffset = 10.0;    // 速率斜率公式里的 −0.1℃ 常数（原软件如此，勿改）
    private const int FlatTimeWindow = 200;         // flat_time_dg：滑窗点数
    private const int FlatTimeAcceptCount = 150;    // flat_time_dg：窗内温差达标点数阈值
    private const int FlatTimeLead = 140;           // flat_time_dg：前置要求点数
    private const int AvgWindow = 15;               // draw_two：均值窗口点数
    private const double IntersectMinDist = 100.0;  // draw_two：终点搜索初始"最小间距"
    private const int IntersectBreakCount = 20;     // draw_two：终点搜索 break 计数（命中后双计数 → 实际只再走 10 个点）

    /// <summary>
    /// 对每次测试逐一计算（每次测试独立状态机，互不影响；输入顺序 = 报告槽位顺序）。
    /// 偏置/结果工位按 input.Station（物理工位）取 —— 测试3 复用工位时不能按数组下标推断。
    /// </summary>
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
        // 物理工位：显式 Station 优先（1|2），0=未指定 → 按槽位下标回退 index+1（兼容旧调用）
        int station = input.Station is 1 or 2 ? input.Station : index + 1;

        // ① 无任何帧 → 未参与
        if (input.Frames.Count == 0)
            return NotParticipated(station, input.WaterMl);

        double tempBias01 = station == 1 ? cal.TempHw1 : cal.TempHw2; // 物理工位1/工位2 各自表面温度偏置

        // ② 复刻接收端：偏置 + 抗抖动，得到"采纳温度序列"（1-based，下标0空置）
        double[] temps = ApplyBiasAndAntiJitter(input.Frames, tempBias01);
        int n = temps.Length - 1; // 有效点数（1-based 最大下标）

        // ③ data_show 状态机（逐帧）
        int pt = 0;               // 当前点号（1-based，每帧 +1）
        int startTimeFlg = 0;     // "盖板打开"见过标记
        int startPoint = -1;      // 起点（盖板 1→0 沿点号），-1=未记录
        int flatDgFlg = 0;        // 斜坡→平台 已判定标记
        int flatStartPoint = 0;   // 平台起始点号
        double[] rate7 = new double[n + 1];    // rate_7point[pt]：斜坡段斜率（窗口=slope_point）
        double[] rate25 = new double[n + 1];   // rate_25point[pt]：平缓段斜率（窗口=flat_point）
        int finishPt = -1;        // flat_time_dg 确认时的点号（-1=未自动结束）

        for (int k = 0; k < n; k++)
        {
            pt = k + 1;
            var frame = input.Frames[k];

            // ---- 起点：盖板"开→闭"沿（MainForm.cs:3002-3008） ----
            if (frame.CoverStatus == 1 && startPoint == -1)
                startTimeFlg = 1;                          // 先见过"打开"才武装
            if (frame.CoverStatus == 0 && startPoint == -1 && startTimeFlg == 1)
                startPoint = pt;                           // 盖上盖板瞬间记起点

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

        if (endPoint <= startPoint) // 终点无效（原软件会算负速率/除零 → 不复刻）
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

    // ============ 终点搜索 draw_two（MainForm.cs:2817-2897） ============

    /// <summary>
    /// draw_two —— 斜坡延长线与平缓线收敛点搜索，返回终点点号（原 textBox10/12）。
    /// 两条延长线的构造在 BuildDrawArrays（报告作图共用同一份，见 BuildDrawLines）。
    /// 口径（照原软件逐行复刻，注意与"两条线的几何交点"不是一回事）：
    /// 在 slope_max+1 .. flat_min−flat_point−2 范围内逐点比 |平缓−斜坡|，只记"间距收进 0.2℃ 且比之前更近"的点；
    /// 原软件 num 命中后每点自增两次，故 num 超过 20 时实际只走满 10 个命中点就早停 ——
    /// 结果因此落在收敛带的前沿，比几何交点早几个点（偏离量随带宽变化：回升越缓带越宽、偏得越多，
    /// 本仓库金标数据是 3 点）。这是原软件的既有口径而非缺陷，且客户验收基准就是设备屏幕上这个数，故原样保留。
    /// </summary>
    private static int DrawTwo(
        Aatcc201CalibrationParams cal, double[] temps, int finishPt, int slopeMax, int flatMin)
    {
        var (slopeDraw, flatDraw, _, _, _) = BuildDrawArrays(
            cal.SlopePoint, cal.FlatPoint, temps, hiBound: finishPt, slopeMax, flatMin);

        // ---- 终点搜索（滚动最小 + 早停，见方法注释） ----
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

    /// <summary>
    /// draw_two 的"两条延长线"构造 —— 原样从 DrawTwo 搬出的唯一真源，终点搜索与报告作图共用，避免"图上那条线"和"算出的终点"两处算法分家。
    /// 斜坡线：slope_max 附近两段 15 点均值连线，按斜率 (a1−a0)÷slope_point 向前延伸，超 37℃ 截断（截断点本身仍填值）；
    /// 平缓线：flat_min 附近两段 15 点均值连线，按斜率 (b1−b0)÷flat_point 向左延伸到 1 号点（右端不高于左端）。
    /// 返回 1-based 数组（下标 0 空置，未填充处为 0）+ 斜坡线实际延伸到的最大点号 + 两线的锚点纵值。
    /// hiBound：向前延伸的点号上界（终点搜索传 finishPt，报告作图传序列末点号）。
    /// </summary>
    private static (double[] SlopeDraw, double[] FlatDraw, int SlopeEnd, double SlopeAnchorY, double FlatAnchorY)
        BuildDrawArrays(
            int slopePoint, int flatPoint, double[] temps, int hiBound, int slopeMax, int flatMin)
    {
        // ---- 斜坡线 ----
        double a0 = Avg15(temps, slopeMax - slopePoint - 7);
        double a1 = Avg15(temps, slopeMax - 7);
        var slopeDraw = new double[hiBound + 1];
        slopeDraw[slopeMax] = a1;
        int slopeEnd = slopeMax;
        for (int i = slopeMax + 1; i < hiBound; i++)
        {
            slopeDraw[i] = slopeDraw[i - 1] + (a1 - a0) / slopePoint;
            slopeEnd = i;
            if (slopeDraw[i] > SlopeLineCeiling01)
                break; // 超温截断（后续点保持 0）
        }

        // ---- 平缓线 ----
        double b0 = Avg15(temps, flatMin - flatPoint - 7);
        double b1 = Avg15(temps, flatMin - 7);
        if (b0 > b1)
            b1 = b0; // 原软件强制平缓线不高于起点
        var flatDraw = new double[hiBound + 1];
        flatDraw[flatMin - flatPoint] = b0;
        for (int i = flatMin - flatPoint - 1; i > 0; i--)
            flatDraw[i] = flatDraw[i + 1] - (b1 - b0) / flatPoint;

        return (slopeDraw, flatDraw, slopeEnd, a0, b1);
    }

    /// <summary>
    /// 报告曲线图上的 draw_two 两条辅助线（横轴 = 点号，纵轴 = 0.01℃）。
    /// 与 DrawTwo 共用 BuildDrawArrays —— 两条线段就是算终点用的那两条，但几何交点比 EndPoint 早几个点（DrawTwo 早停口径所致，见其注释；Aatcc201DrawLinesTests 锁的是 2% 跨度容差与"终点落在收敛带内"，不是等号）。
    /// temps01：采纳后温度序列（SurfaceTempSeries 那串，下标 0 = 1 号点）。
    /// 输入退化（无序列 / slope_point、flat_point 非正 / 锚点窗口取不出）时返回 null，调用方按"不画线"处理。
    /// </summary>
    public static Aatcc201DrawLines? BuildDrawLines(
        IReadOnlyList<double>? temps01, int slopePoint, int flatPoint, int slopeMax, int flatMin)
    {
        if (temps01 is not { Count: > 0 } || slopePoint <= 0 || flatPoint <= 0)
            return null;

        int n = temps01.Count;
        // 锚点窗口必须落有点：斜坡锚点 slopeMax−slope_point ≥ 1、平缓锚点 flatMin−flat_point ≥ 1；且两者都在序列内
        if (slopeMax > n || slopeMax - slopePoint < 1 || flatMin > n || flatMin - flatPoint < 1)
            return null;

        var temps = new double[n + 1]; // 转 1-based（与 Calculate 内部同构），下标 0 空置
        for (int k = 0; k < n; k++)
            temps[k + 1] = temps01[k];

        var (slopeDraw, flatDraw, slopeEnd, slopeAnchorY, flatAnchorY) =
            BuildDrawArrays(slopePoint, flatPoint, temps, hiBound: n, slopeMax, flatMin);
        return new Aatcc201DrawLines(
            SlopeA: (slopeMax - slopePoint, slopeAnchorY),
            SlopeB: (slopeEnd, slopeDraw[slopeEnd]),
            FlatA: (1, flatDraw[1]),
            FlatB: (flatMin, flatAnchorY));
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
