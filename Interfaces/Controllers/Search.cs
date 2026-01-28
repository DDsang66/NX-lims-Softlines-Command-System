using Microsoft.AspNetCore.Mvc;
using FuzzySharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Diagnostics;
using NX_lims_Softlines_Command_System.Domain.Model;
using System.Security.Cryptography;
using System.Net.Sockets;
using System.Net;

namespace NX_lims_Softlines_Command_System.Interfaces.Controllers
{

    [ApiController]
    [Route("api/search")]
    public class SearchController : ControllerBase
    {

        private readonly IWebHostEnvironment _env;
        private readonly LabDbContextSec _db;
        public SearchController(LabDbContextSec db,IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
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
            var csList = _db.CustomerServices
                .Select(cs => new { cs.Id, cs.CustomerService1 })
                .Distinct()
                .ToList();
            return Ok(new { success = true, message = "CS Load Succeed", data = csList });
        }

        [HttpGet("getUser")]
        public IActionResult getUser()
        {
            var userList = _db.Users
                .Select(cs => new { cs.UserId, cs.NickName })
                .Distinct()
                .ToList();
            return Ok(new { success = true, message = "User Load Succeed", data = userList });
        }

        [HttpGet("getExcelUrl")]
        public async Task<IActionResult> GetExcelUrl(string repo,string buyer,string group,[FromServices] IHttpContextAccessor httpContext)
        {
            if (string.IsNullOrEmpty(repo) || string.IsNullOrEmpty(buyer) || string.IsNullOrEmpty(group))return Ok(new { success = false, message = "参数不能为空", data = "null" });

            if (group.ToLower() == "physics") group = "PHY";

            var request = httpContext.HttpContext!.Request;

            // 自动获取当前请求的 scheme + host + port
            var baseUrl = $"{request.Scheme}://{request.Host}";

            // 或只拿本机 IP（如果 Docker 用 host.docker.internal）
            var localIP = GetLocalIPAddress();

            var host = request.Host.Host;  // 192.168.74.8 或 localhost

            var port = request.Host.Port;  // 5051

            var baseAddress = $"http://{localIP}:{port}";

            var fileName = $"{buyer}_{group.ToUpper()}_sheet.xlsx";

            var filePath = Path.Combine(_env.WebRootPath, "ExcelModel", fileName);

            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);

            using var md5 = MD5.Create();

            var hash = md5.ComputeHash(fileBytes);

            var hashString = BitConverter.ToString(hash).Replace("-", "").Substring(0, 16);

            var fileKey = $"{repo}_{hashString}";

            return Ok(new
            {
                fileKey = fileKey,
                fileName = Path.GetFileName(filePath),
                downloadUrl = $"{baseAddress}/api/search/{fileName}/download",
                callbackUrl = $"{baseAddress}/api/search/{fileName}/callback"
            });
        }

        private string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            return "127.0.0.1";
        }

        [HttpGet("{fileName}/download")]
        public IActionResult Download(string fileName)
        {
            // 根据 fileId 找到实际文件路径

            var filePath = Path.Combine(_env.WebRootPath, "ExcelModel", fileName);
            // 返回文件流，Content-Type 必须正确
            return PhysicalFile(
                filePath,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileDownloadName: fileName,
                enableRangeProcessing: true  // 支持断点续传
            );
        }

        [HttpPost("{fileName}/callback")]
        public async Task<IActionResult> Callback(/*string fileId, [FromBody] CallbackData data*/)
        {
            //// status: 0=无变化, 2=准备保存, 6=保存完成
            //if (data.Status == 2 || data.Status == 6)
            //{
            //    // Document Server 给了新文件 URL，下载回来
            //    using var client = new HttpClient();
            //    var newFileBytes = await client.GetByteArrayAsync(data.Url);

            //    // 覆盖原文件
            //    var filePath = FindFileById(fileName);
            //    await System.IO.File.WriteAllBytesAsync(filePath, newFileBytes);
            //}

            return Ok("{ \"error\": 0 }");  // 必须返回这个
        }


    }
}
