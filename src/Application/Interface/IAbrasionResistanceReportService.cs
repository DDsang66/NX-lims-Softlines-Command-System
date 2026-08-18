using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.AbrasionResistance_FW_.NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.AbrasionResistance_FW_;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Application.Interface
{
    public interface IAbrasionResistanceReportService:IScopedDependency
    {
        /// <summary>
        /// Generates an abrasion resistance report based on the provided BuildReportDto and returns a Result containing a DocxUrlResponseDto.
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        Result<DocxUrlResponseDto> Generate(BuildReportDto dto);
    }
}
