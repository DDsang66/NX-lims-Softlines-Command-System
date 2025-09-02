using Microsoft.AspNetCore.Mvc;
using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.Models;

namespace NX_lims_Softlines_Command_System.Application.Services.Interfaces
{
    public interface IBuyer
    {
        Task<object?> ShowItem([FromBody] RequiredInfoDto infoDto);
        Task<object?> ShowParameter([FromBody] RequiredInfoDto infoDto);

    }
}
