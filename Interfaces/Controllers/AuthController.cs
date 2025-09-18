using Azure;
using Microsoft.AspNet.Identity;
using Microsoft.AspNetCore.Mvc;
using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.Application.Services.AuthenticationService;
using NX_lims_Softlines_Command_System.Domain.Model;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;
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
                user = reviewer,
                id = user.UserId
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
                return Ok(new{ success = true,data = newTokens });
            }
            catch
            {
                return Unauthorized();
            }
        }

        [HttpPost("register")]
        public IActionResult Register([FromBody] RegisterRequest req)
        {
            var user = _db.Users.FirstOrDefault(u => u.UserName == req.Email);
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
                    EmployeeId = req.EmployeeId,
                    PermissionIndex = 1,   //权限映表索引
                    CreateTime = DateTime.Now,
                    Status = 1,         //启用
                    LoginFailCount = 0
                };

                var newUserProfile = new UserProfile
                {
                    EmployeeId = req.EmployeeId,
                    RealName = req.NickName,
                };
                _db.Add(newUser);
                _db.Add(newUserProfile);
                _db.SaveChanges();
                return Ok(new { success = true});
            }
            catch
            {
                // 记录异常信息，便于调试
                return StatusCode(500, "注册失败，请联系管理员");
            }

        }

        [HttpPost("pwdreset")]
        public IActionResult pwdReset([FromBody] PwdReset req)
        {
            // 检查请求对象是否为null
            if (req == null)
            {
                return BadRequest("Invalid request data."); // 返回400 Bad Request
            }

            // 检查请求中的必要字段是否为空
            if (string.IsNullOrWhiteSpace(req.AuthenticInfo) || string.IsNullOrWhiteSpace(req.NewPassword))
            {
                return BadRequest("AuthenticInfo and NewPassword are required."); // 返回400 Bad Request
            }

            var user = _db.Users.FirstOrDefault(u => u.UserName == req.AuthenticInfo || u.NickName == req.AuthenticInfo || u.EmployeeId == req.AuthenticInfo);
            if (user == null) return StatusCode(500, "User not found."); // 返回500 Internal Server Error

            PasswordHasher ph = new PasswordHasher();
            user.PassWord = ph.HashPassword(req.NewPassword);
            _db.Update(user);
            _db.SaveChanges();

            return Ok(new { success = true });
        }

        private User? ValidateUser(string username, string pwd)
        {
            PasswordHasher ph = new PasswordHasher();
            var user = _db.Users.FirstOrDefault(u => u.UserName == username && u.Status == 1);
            if (user == null) return null;
            bool isPasswordCorrect =
                ph.VerifyHashedPassword(user.PassWord, pwd) == PasswordVerificationResult.Success;

            if (isPasswordCorrect)
            {
                user.LoginFailCount = 0;// 重置失败计数
                user.UpdatedTime = DateTime.Now;
            }
            else
            {
                user.LoginFailCount += 1;
                if (user.LoginFailCount >= 5)
                    user.Status = 0;       // 锁定
            }
            _db.SaveChanges();

            return isPasswordCorrect ? user : null;
        }
    }
}
