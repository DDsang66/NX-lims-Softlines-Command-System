using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.YarnCountContext;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Application.Interface.YarnCountContext;

/// <summary>纱支报告生成服务接口</summary>
public interface IYarnCountReportService : IScopedDependency
{
    Result<DocxUrlResponseDto> Generate(YarnCountReportRequestDto dto);
}
