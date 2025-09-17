using Microsoft.AspNetCore.Mvc;
using FuzzySharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Diagnostics;
using NX_lims_Softlines_Command_System.Domain.Model;

namespace NX_lims_Softlines_Command_System.Interfaces.Controllers
{

    [ApiController]
    [Route("api/search")]
    public class SearchController : ControllerBase
    {

        private readonly LabDbContextSec _db;
        public SearchController(LabDbContextSec db)
        {
            _db = db;
        }

        [HttpPost("main")]
        public IActionResult Index([FromBody] string searchQuery)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchQuery))
                {
                    return Ok(new { success = false, message = "Search query is empty", data = "null" });
                }

                var allowedActions = new HashSet<string> { "Mango", "CrazyLine", "Adidas", "Jako", "Index" };
                var bestMatch = FuzzySharp.Process.ExtractOne
                    (searchQuery, allowedActions, cutoff: 70); // 相似度阈值 (0-100)，越高越严格

                if (bestMatch.Score >= 70 && !allowedActions.Contains(searchQuery))
                {
                    // 提示用户是否要跳转到匹配的 Action
                    string suggestedAction = bestMatch.Value;
                    return Ok(new { success = true, message = "Did you mean?", data = suggestedAction });
                }
                // 完全匹配或用户确认后跳转
                else if (allowedActions.Contains(searchQuery))
                {
                    return Ok(new { success = true, message = "Match found", data = searchQuery });
                }
                else
                {
                    return Ok(new { success = false, message = "No match found", data = searchQuery });
                }
            }
            catch (Exception ex)
            {
                // 记录错误日志
                Console.WriteLine($"Error in SearchController.Index: {ex.Message}");
                return Ok(new { success = false, message = "An error occurred", data = searchQuery });
            }
        }



        [HttpGet("getCs")]
        public IActionResult getCs()
        {
            var csList = _db.CustomerServices.Select(cs => cs.CustomerService1).Distinct().ToList();
            return Ok(new {status = 1 ,success = true, message = "CS Load Succeed",data = csList});
        }
    }
}
