using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.MoistureDryingRateContext;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Application.Interface.MoistureDryingRateContext;

/// <summary>
/// 干燥速率报告生成应用服务接口（POST report/nf5022 | report/aatcc201）。
/// 直接用前端 POST 的计算结果组数据（无 DB 读取环节）, 生成 DOCX 存服务器报告目录, 返回下载链接。
/// 通过 IScopedDependency 自动注册。
/// </summary>
public interface IMoistureDryingRateReportService : IScopedDependency
{
    /// <summary>NF5022: 内嵌权威结果 → DryingRate.docx 报告文件。</summary>
    Result<DocxUrlResponseDto> GenerateNf5022(Nf5022ReportRequestDto dto);

    /// <summary>AATCC 201: 内嵌权威结果 → Aatcc201.docx 报告文件（回显校准参数需读库, 故 async）。</summary>
    Task<Result<DocxUrlResponseDto>> GenerateAatcc201(Aatcc201ReportRequestDto dto, CancellationToken ct);

    /// <summary>
    /// AATCC 201 合并报告: 历史报告界面勾选的同一报告号下多个样品文件 → 一份报告（一个样品一张 Sample 表）。
    /// 数据源是所选 docx 本身(解析读回), 不重算; 产物落同一报告目录, 可再被选中继续合并。
    /// </summary>
    Result<DocxUrlResponseDto> GenerateAatcc201Combined(Aatcc201CombineRequestDto dto);

    /// <summary>AATCC 201 历史报告列表（含从报告 docx 回填的样品名称, 供界面样品名列）。</summary>
    Result<List<ReportFileMeta>> ListAatcc201Reports(string? keyword);
}
