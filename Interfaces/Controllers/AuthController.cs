using Microsoft.AspNet.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.Application.Services.AuthenticationService;
using NX_lims_Softlines_Command_System.Domain.Model;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;
using NX_lims_Softlines_Command_System.Models;
using System.IdentityModel.Tokens.Jwt;

namespace NX_lims_Softlines_Command_System.Interfaces.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly JwtService _jwt;
        private readonly LabDbContextSec _db;
        public AuthController(JwtService jwt, LabDbContextSec db)
        {
            _jwt = jwt;
            _db = db;
        }
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest req)
        {
            // 1. 查库验证
            var user = ValidateUser(req.Email, req.Password);
            if (user == null) return Unauthorized("账号或密码错误");

            // 2. 发令牌
            var tokens = _jwt.GenerateTokens(user.UserId.ToString());
            string reviewer = user.NickName!;
            var response = new
            {
                tokens = tokens,
                reviewer = reviewer
            };
            return Ok(response);
        }

        [HttpPost("refresh")]
        public IActionResult Refresh([FromBody] string refreshToken)
        {
            var handler = new JwtSecurityTokenHandler();
            try
            {
                var token = handler.ReadJwtToken(refreshToken);
                if (token.ValidTo < DateTime.UtcNow) return Unauthorized();

                var userId = token.Claims.First(c => c.Type == "uid").Value;
                var newTokens = _jwt.GenerateTokens(userId);
                return Ok(newTokens);
            }
            catch
            {
                return Unauthorized();
            }
        }

        [HttpPost("register")]
        public IActionResult Register([FromBody] RegisterRequest req)
        {
            var user = _db.User.FirstOrDefault(u => u.UserName == req.Email);
            if (user != null) return Unauthorized("账户已存在");
            try
            {
                PasswordHasher ph = new PasswordHasher();
                string hasPwd = ph.HashPassword(req.Password);

                // 使用雪花算法生成唯一 ID
                var snowflake = new SnowflakeIdGenerator();
                long userId = snowflake.NextId();
                var newUser = new User
                {
                    UserId = userId.ToString(),
                    UserName = req.Email,
                    PassWord = hasPwd,
                    NickName = req.NickName,
                    EmployeeId = "Default"

                };
                _db.Add(newUser);
                _db.SaveChanges();
                return Ok();
            }
            catch
            {
                // 记录异常信息，便于调试
                return StatusCode(500, "注册失败，请联系管理员");
            }

        }

        private User? ValidateUser(string username, string pwd)
        {
            PasswordHasher ph = new PasswordHasher();
            var user = _db.User.FirstOrDefault(u => u.UserName == username);
            if (user == null) return null;
            bool isPasswordCorrect = ph.VerifyHashedPassword(user.PassWord, pwd) == PasswordVerificationResult.Success;
            if (isPasswordCorrect)
            {
                return user!;
            }
            else { return null; }
        }
    }
}
