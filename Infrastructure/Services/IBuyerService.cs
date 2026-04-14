using Microsoft.AspNetCore.Mvc;
using NX_lims_Softlines_Command_System.Application.DTO;

namespace NX_lims_Softlines_Command_System.Infrastructure.Services
{
    public interface IBuyerService
    {
        /// <summary>
        /// Show Item
        /// </summary>
        /// <param name="infoDto"></param>
        /// <returns></returns>
        Task<object?> ShowItemAsync([FromBody] RequiredInfoDto infoDto);


        /// <summary>
        /// Show Parameter
        /// </summary>
        /// <param name="infoDto"></param>
        /// <returns></returns>
        Task<object?> ShowParameterAsync([FromBody] RequiredInfoDto infoDto);
    }
}
