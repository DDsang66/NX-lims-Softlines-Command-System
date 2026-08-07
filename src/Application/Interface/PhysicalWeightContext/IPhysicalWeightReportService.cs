using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.PhysicalWeightContext;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Application.Interface.PhysicalWeightContext;

/// <summary>物理克重报告生成服务接口</summary>
public interface IPhysicalWeightReportService : IScopedDependency
{
    Result<DocxUrlResponseDto> Generate(PhysicalWeightReportRequestDto dto);
}
