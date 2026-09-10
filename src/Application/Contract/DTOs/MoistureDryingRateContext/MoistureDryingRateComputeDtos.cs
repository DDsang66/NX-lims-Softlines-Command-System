namespace NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.MoistureDryingRateContext;

// ============================================================
// 干燥速率"权威计算"请求/结果 DTO（POST compute/nf5022 | compute/aatcc201）
//
// 定位:
//   - 前端测试结束把原始时序 POST 过来, 后端 C# 做权威计算（可单测）, 不落结构化库。
//   - 请求头带样品/报告号, 供"下一步 POST report 生成 DOCX"直接复用同一份输入。
//   - 结果里两个速率字段:
//        RateMgPerHour = 存储原值（mg/h, 决策B: 蒸发曲线回归斜率, 替代原中点公式）
//        RateGPerHour  = 报告显示值（g/h = RateMgPerHour ÷ 1000, 决策7: "存 mg/h 报告 g/h"）
// ============================================================

// ────────────────────── NF5022（重量式, 6 工位） ──────────────────────

/// <summary>NF5022 权威计算请求：样品头 + 整机参数 + 每工位原始时序。</summary>
public class Nf5022ComputeRequestDto
{
    /// <summary>报告号（手动输入, 文件名首段）</summary>
    public string ReportNumber { get; set; } = string.Empty;

    /// <summary>样品名称</summary>
    public string SampleName { get; set; } = string.Empty;

    /// <summary>环境温度（原软件文本输入）</summary>
    public string Temperature { get; set; } = string.Empty;

    /// <summary>环境湿度（原软件文本输入）</summary>
    public string Humidity { get; set; } = string.Empty;

    /// <summary>采样间隔(分钟), 设备回帧 @n 中的 n, 通常 3。终止判定/蒸发时间/残留率公式都依赖它。</summary>
    public int SpaceTimeMin { get; set; }

    /// <summary>水分残留率检测时刻(min), 原软件 textBox11, 默认 30。</summary>
    public int ResidualMinute { get; set; }

    /// <summary>
    /// 测试方法: 0=GBT 21655.1 2008, 1=GBT 21655.1 2023（原软件 comboBox2）。
    /// 默认 1=2023 —— 照原软件 comboBox2 构造时 SelectedIndex=1(默认选中 GBT 21655.1 2023):
    /// 请求 JSON 没带该字段时走 2023, 显式传 0 才用 2008。
    /// </summary>
    public int TestMethod { get; set; } = 1;

    /// <summary>6 工位原始时序（未参与工位传空列表即可, 不会产生结果）</summary>
    public List<Nf5022StationComputeInputDto> Stations { get; set; } = new();
}

/// <summary>NF5022 单工位计算输入：测试前称得的架重/干布重 + 测试期间按采样时序收到的原始重量(mg)。</summary>
public class Nf5022StationComputeInputDto
{
    /// <summary>架重(mg), 去皮称重（原 button13）</summary>
    public double FrameWeightMg { get; set; }

    /// <summary>干布重(mg) = 称重 − 架重（原 button2 等逐站称干布）</summary>
    public double ClothWeightMg { get; set; }

    /// <summary>测试期间逐点原始重量(mg), 首点用于定死滴水量 water=raw[0]−架−布</summary>
    public List<double> RawWeightMg { get; set; } = new();
}

/// <summary>NF5022 权威计算结果（不落库, 直接回给前端刷新结果表）。</summary>
public class Nf5022ComputeResultDto
{
    /// <summary>采样间隔(分钟) 回显</summary>
    public int SpaceTimeMin { get; set; }

    /// <summary>残留检测时刻(min) 回显</summary>
    public int ResidualMinute { get; set; }

    /// <summary>测试方法 0/1 回显</summary>
    public int TestMethod { get; set; }

    /// <summary>每工位结果（6 项, 未参与工位 Participated=false）</summary>
    public List<Nf5022StationResultDto> Stations { get; set; } = new();
}

/// <summary>NF5022 单工位权威计算结果（与 DryingRateCalculationService.Nf5022StationResult 对应）。</summary>
public class Nf5022StationResultDto
{
    /// <summary>工位号 1..6</summary>
    public int Station { get; set; }

    /// <summary>是否参与测试（有首点且滴水量&gt;0）。未参与时其余字段全 0。</summary>
    public bool Participated { get; set; }

    /// <summary>滴水量(mg) = 首点原始重量 − 架重 − 干布重</summary>
    public double WaterMg { get; set; }

    /// <summary>干布重(mg) = 称重 − 架重（GB21655 报告 m0 = 试样原始质量(g) 的数据源）</summary>
    public double ClothWeightMg { get; set; }

    /// <summary>终止点数（触发结束条件时的采样点数, 原软件 result_point）</summary>
    public int ResultPoint { get; set; }

    /// <summary>蒸发时间(min) = 采样间隔 × (终止点数 − 1)</summary>
    public double TimeMin { get; set; }

    /// <summary>干燥速率存储原值(mg/h) = 蒸发曲线回归斜率（决策B, 替代原中点公式）</summary>
    public double RateMgPerHour { get; set; }

    /// <summary>干燥速率报告值(g/h) = RateMgPerHour ÷ 1000（决策7）</summary>
    public double RateGPerHour { get; set; }

    /// <summary>残留率(‰) = 1000 − 指定时刻蒸发量×1000 ÷ 滴水量</summary>
    public double SfclPermille { get; set; }

    /// <summary>蒸发曲线(mg), 首点为 0; 长度=采样点数。报告嵌入曲线图用。</summary>
    public List<double>? EvaporationCurveMg { get; set; }
}

// ────────────────────── AATCC 201（加热板法, 2 工位） ──────────────────────

/// <summary>AATCC 201 权威计算请求：样品头 + 每工位温度时序。</summary>
public class Aatcc201ComputeRequestDto
{
    /// <summary>报告号（手动输入, 文件名首段）</summary>
    public string ReportNumber { get; set; } = string.Empty;

    /// <summary>样品名称</summary>
    public string SampleName { get; set; } = string.Empty;

    /// <summary>环境温度（原软件文本输入）</summary>
    public string Temperature { get; set; } = string.Empty;

    /// <summary>环境湿度（原软件文本输入）</summary>
    public string Humidity { get; set; } = string.Empty;

    /// <summary>
    /// 每次测试的温度时序（1..3 项, 槽位对齐: 列表第 i 项 = 报告 #(i+1)）。
    /// #1=工位1 首次测试、#2=工位2 首次测试、#3=测试3(任选工位重测)。
    /// 未测的中间槽也要占位: 传空 Frames(→ Participated=false → 报告该行留空), 否则会挤位。
    /// </summary>
    public List<Aatcc201StationComputeInputDto> Stations { get; set; } = new();
}

/// <summary>
/// AATCC 201 单次测试计算输入：物理工位 + 滴水量 + 逐帧温度/盖板时序。
/// Station 是本次测试实际用的物理工位(1|2)——测试3 可能复用工位1 或工位2,
/// 面温偏置(TempHw1/TempHw2)必须按它取, 不能按列表下标推断。
/// </summary>
public class Aatcc201StationComputeInputDto
{
    /// <summary>
    /// 物理工位 1|2（本项所在报告槽位 #1→1、#2→2、#3→重测选用的工位）。
    /// 0 = 未指定, 由计算服务按槽位下标回退(index+1)。
    /// </summary>
    public int Station { get; set; }

    /// <summary>滴水量(mL), 原软件 textBox8, 默认 0.2。速率公式分子。</summary>
    public double WaterMl { get; set; }

    /// <summary>设备遥测帧序列（时间序, 逐帧记录温度/盖板状态）</summary>
    public List<Aatcc201FrameSampleDto> Frames { get; set; } = new();
}

/// <summary>AATCC 201 单帧遥测采样（与 Aatcc201FrameSample 对应, 温度 0.01℃ 单位整数）。</summary>
public class Aatcc201FrameSampleDto
{
    /// <summary>表面温度1 原始值(0.01℃ 单位整数, 如 3700=37.00℃)</summary>
    public int SurfaceRaw01 { get; set; }

    /// <summary>加热板温度1 原始值(0.01℃ 单位整数; 偏置叠加在设备侧或前端, 计算只按差值用)</summary>
    public int BoardRaw01 { get; set; }

    /// <summary>盖板状态 0=开 1=闭（闭→开沿用于记起点）</summary>
    public int CoverStatus { get; set; }

    /// <summary>本帧到达的真实秒数（相对测试开始; 步骤5 帧率校准后用于换算真实时间）</summary>
    public double FrameTimeSec { get; set; }
}

/// <summary>AATCC 201 权威计算结果（不落库, 直接回给前端刷新结果表）。</summary>
public class Aatcc201ComputeResultDto
{
    /// <summary>
    /// 每次测试结果（槽位对齐, 最多 3 项; 第 i 项 = 报告 #(i+1), 未参与测试 Participated=false）。
    /// </summary>
    public List<Aatcc201StationResultDto> Stations { get; set; } = new();
}

/// <summary>
/// AATCC 201 采纳温度点（对应 Aatcc201TempPoint; 报告曲线图数据源）。
/// 点序=采样序, 与结果 StartPoint/EndPoint 同坐标系。
/// </summary>
public class Aatcc201TempPointDto
{
    /// <summary>本帧真实到达秒（测试开始起; 步骤5帧率校准后作横轴/换算时间用）</summary>
    public double FrameTimeSec { get; set; }

    /// <summary>采纳后表面温度(0.01℃ 单位整数, 已叠 temp_hw 偏置)</summary>
    public double SurfaceTemp01 { get; set; }
}

/// <summary>AATCC 201 单工位权威计算结果（与 Aatcc201StationResult 对应）。</summary>
public class Aatcc201StationResultDto
{
    /// <summary>本次测试实际使用的物理工位 1|2（测试3 复用工位时, 工位号可能重复出现）</summary>
    public int Station { get; set; }

    /// <summary>是否参与（有盖板闭→开沿 + 有平台终点 + 交点合法）。未参与时其余字段全 0。</summary>
    public bool Participated { get; set; }

    /// <summary>干燥速率存储原值(mg/h, 整数) = (int)(水mL×1000 ÷(终点−起点) ×3600 + 0.5)</summary>
    public double RateMgPerHour { get; set; }

    /// <summary>干燥速率报告值(g/h) = RateMgPerHour ÷ 1000（决策7）</summary>
    public double RateGPerHour { get; set; }

    /// <summary>起点采样序号（盖板 1→0 沿）</summary>
    public int StartPoint { get; set; }

    /// <summary>终点采样序号（draw_two 斜坡/平缓线交点）</summary>
    public int EndPoint { get; set; }

    /// <summary>斜坡最大点（draw_two 斜坡线的锚点）</summary>
    public int SlopeMaxPoint { get; set; }

    /// <summary>平缓最小点（draw_two 平缓线的锚点）</summary>
    public int FlatMinPoint { get; set; }

    /// <summary>干燥时间(秒) = 终点 − 起点（每点按 1 秒; 真机帧率校准见步骤5）</summary>
    public double DryingTimeSec { get; set; }

    /// <summary>滴水量(mL) 回显</summary>
    public double WaterMl { get; set; }

    /// <summary>
    /// 采纳后温度曲线（已叠偏置+抗抖动, 点序=采样序）。
    /// 仅参与工位非空; 未参与工位为 null。报告嵌入曲线图的数据源（POST report 时随结果整体回传）。
    /// </summary>
    public List<Aatcc201TempPointDto>? SurfaceTempSeries { get; set; }
}
