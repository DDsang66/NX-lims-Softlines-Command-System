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
}
