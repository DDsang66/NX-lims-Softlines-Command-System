using Microsoft.AspNetCore.Mvc;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;

namespace NX_lims_Softlines_Command_System.Interfaces.Controllers
{
    [ApiController]
    [Route("api/profile")]
    public class UserProfileController : ControllerBase
    {
        [HttpPost("edit")]
        public IActionResult Edit([FromBody] UserProfile dto)
        {
            return Ok();
        }

        [HttpGet("render")]
        public IActionResult Render(string reviewer)
        {
            return Ok();
        }
    }
}
