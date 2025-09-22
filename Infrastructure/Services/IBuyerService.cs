using Microsoft.AspNetCore.Mvc;
using NX_lims_Softlines_Command_System.Application.DTO;

namespace NX_lims_Softlines_Command_System.Infrastructure.Services
{
    public interface IBuyerService
    {
        Task<object?> ShowItemAsync([FromBody] RequiredInfoDto infoDto);
        Task<object?> ShowParameterAsync([FromBody] RequiredInfoDto infoDto);
    }
}
