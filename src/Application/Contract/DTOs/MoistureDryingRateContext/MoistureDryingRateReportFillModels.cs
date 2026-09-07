namespace NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.MoistureDryingRateContext;

// ============================================================
// 干燥速率报告"填充模型" — 报告服务计算后的纯数据载体
// 引擎(IDryingRateDocxEngine / IAatcc201DocxEngine)只接收模型, 不依赖 OpenXml 类型。
// 照 PhysicalWeightReportFillModel 约定: 与 DTO 同命名空间, 结构由引擎 Layout 常量对号入座。
// ============================================================

/// <summary>NF5022(DryingRate.docx) 报告填充模型。</summary>
public class DryingRateReportFillModel
{
    /// <summary>报告号</summary>
    public string ReportNumber { get; set; } = string.Empty;

    /// <summary>样品名称</summary>
    public string SampleName { get; set; } = string.Empty;

    /// <summary>环境温度</summary>
    public string Temperature { get; set; } = string.Empty;

    /// <summary>环境湿度</summary>
    public string Humidity { get; set; } = string.Empty;

    /// <summary>测试方法文字: "GBT 21655.1 2008" | "GBT 21655.1 2023"</summary>
    public string TestMethod { get; set; } = string.Empty;

    /// <summary>采样间隔(分钟) 回显</summary>
    public int SpaceTimeMin { get; set; }

    /// <summary>残留检测时刻(min) 回显</summary>
    public int ResidualMinute { get; set; }

    /// <summary>逐工位结果行（6 项, 未参与工位 Participated=false, 引擎留空格）</summary>
    public List<DryingRateStationRowModel> Stations { get; set; } = new();

    /// <summary>
    /// 蒸发曲线图 PNG 集合 —— **每个参与工位(样品)独立一张**(6 线不再挤在一张里),
    /// 顺序 = 报告服务按参与工位升序生成, 每张图内自带头"样品N 蒸发曲线"。
    /// 引擎按序把每张追加到文档末尾; 某工位渲染失败/无曲线 → 该项不生成(引擎只见存在的)。
    /// </summary>
    public List<byte[]> ChartPngs { get; set; } = new();
}

/// <summary>NF5022 报告单工位行（对应 Nf5022StationResultDto 的报告视图）。</summary>
public class DryingRateStationRowModel
{
    public int Station { get; set; }

    public bool Participated { get; set; }

    /// <summary>滴水量(mg)</summary>
    public double WaterMg { get; set; }

    /// <summary>干布重(mg)（GB21655 报告 m0 = 试样原始质量(g) 的数据源）</summary>
    public double ClothWeightMg { get; set; }

    /// <summary>蒸发时间(min)</summary>
    public double TimeMin { get; set; }

    /// <summary>干燥速率存储值(mg/h)</summary>
    public double RateMgPerHour { get; set; }

    /// <summary>干燥速率报告值(g/h) = mg/h ÷ 1000</summary>
    public double RateGPerHour { get; set; }

    /// <summary>残留率(‰)</summary>
    public double SfclPermille { get; set; }

    /// <summary>
    /// 蒸发曲线(mg), 首点 0; 长度=采样点数。
    /// GB21655 报告按模板 0/3/6..60min 网格填 Δmi/mi、并算回归斜率的数据源。未参与工位为 null。
    /// </summary>
    public List<double>? EvaporationCurveMg { get; set; }
}

/// <summary>AATCC 201(Aatcc201.docx) 报告填充模型。</summary>
public class Aatcc201ReportFillModel
{
    /// <summary>报告号</summary>
    public string ReportNumber { get; set; } = string.Empty;

    /// <summary>样品名称</summary>
    public string SampleName { get; set; } = string.Empty;

    /// <summary>环境温度</summary>
    public string Temperature { get; set; } = string.Empty;

    /// <summary>环境湿度</summary>
    public string Humidity { get; set; } = string.Empty;

    /// <summary>
    /// 校准参数展示文字（斜坡点/平缓点/持续温度等）。
    /// 当前为空, 待定: 报告是否/如何回显校准参数（需前端或结果 DTO 补充该数据）。
    /// </summary>
    public string CalibrationText { get; set; } = string.Empty;

    /// <summary>逐工位结果行（2 项, 未参与工位 Participated=false, 引擎留空格）</summary>
    public List<Aatcc201StationRowModel> Stations { get; set; } = new();

    /// <summary>
    /// 温度曲线图 PNG（图表库决策未定时为 null → 引擎不插图）。
    /// 曲线数据源已就绪: 各工位行的 SurfaceTempSeries（采纳后温度曲线, 已叠偏置+抗抖动）。
    /// 图表库决策后在此生成单张 PNG（两工位曲线同图, 照原软件 report_mod.xls 曲线 Sheet 布局）。
    /// </summary>
    public byte[]? ChartImagePng { get; set; }
}

/// <summary>AATCC 201 报告单工位行（对应 Aatcc201StationResultDto 的报告视图）。</summary>
public class Aatcc201StationRowModel
{
    public int Station { get; set; }

    public bool Participated { get; set; }

    /// <summary>滴水量(mL)</summary>
    public double WaterMl { get; set; }

    /// <summary>干燥时间(s) = 终点 − 起点</summary>
    public double DryingTimeSec { get; set; }

    /// <summary>干燥速率存储值(mg/h)</summary>
    public double RateMgPerHour { get; set; }

    /// <summary>干燥速率报告值(g/h) = mg/h ÷ 1000</summary>
    public double RateGPerHour { get; set; }

    /// <summary>起点采样序号</summary>
    public int StartPoint { get; set; }

    /// <summary>终点采样序号</summary>
    public int EndPoint { get; set; }

    /// <summary>
    /// 采纳后温度曲线（已叠偏置+抗抖动, 点序=采样序; 未参与工位为 null）。
    /// 报告服务用它在图表库决策后生成温度曲线 PNG（当前 ChartImagePng 仍由图表库未定而恒为 null）。
    /// </summary>
    public List<Aatcc201TempPointDto>? SurfaceTempSeries { get; set; }
}
