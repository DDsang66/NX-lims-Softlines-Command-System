using Microsoft.AspNetCore.Mvc;
using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.Domain.Model;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;

namespace NX_lims_Softlines_Command_System.Interfaces.Controllers
{
    [ApiController]
    [Route("api/profile")]
    public class UserProfileController : ControllerBase
    {
        private readonly LabDbContextSec _dbContextSec;
        public UserProfileController(LabDbContextSec dbContextSec)
        {
            _dbContextSec = dbContextSec;
        }


        [HttpPost("edit")]
        public IActionResult Edit([FromBody] ProfileEdit dto)
        {
            var profile = _dbContextSec.UserProfiles.FirstOrDefault(p => p.RealName == dto.RealName && p.EmployeeId ==dto.EmployeeId);
            if (profile == null) return NotFound("Profile not found");
            profile.Phone = dto.Phone;
            profile.Email = dto.Email;
            profile.Birth = DateOnly.Parse(dto.Birth);
            profile.IdCard = dto.IdCard;
            _dbContextSec.UserProfiles.Update(profile);
            _dbContextSec.SaveChanges();

            return Ok();
        }

        [HttpPost("render")]
        public IActionResult Render([FromBody] string user)
        {
            var profile = _dbContextSec.UserProfiles.FirstOrDefault(p => p.RealName == user);
            if (profile == null) return NotFound("Profile not found");
            var profileData = new
            {
                name = profile.RealName,
                email = profile.Email,
                phone = profile.Phone,
                employeeId = profile.EmployeeId,
                gender = profile.Gender,
                birth = profile.Birth
            };

            return Ok(profileData);
        }


        [HttpGet("avatar")]
        public IActionResult Avatar([FromBody] string user)
        {
            var profile = _dbContextSec.UserProfiles.FirstOrDefault(p => p.RealName == user);
            var contentType = "image/jpeg"; // 根据你的默认头像文件类型调整
            if (profile == null)
            {
                // 如果用户不存在，返回404
                return NotFound();
            }
            if (string.IsNullOrEmpty(profile.AvatarUrl))
            {
                // 如果头像URL为空，使用默认头像
                var defaultAvatarPath = Path.Combine("wwwroot", "Avatar", "default-avatar.jpg");
                var defaultAvatarPhysicalPath = Path.Combine(Directory.GetCurrentDirectory(), defaultAvatarPath);

                if (!System.IO.File.Exists(defaultAvatarPhysicalPath))
                {
                    // 如果默认头像文件不存在，返回404
                    return NotFound();
                }

                // 返回默认头像
                return PhysicalFile(defaultAvatarPhysicalPath, contentType);
            }

            // 如果头像URL不为空，返回头像二进制数据
            var avatarPhysicalPath = Path.Combine(Directory.GetCurrentDirectory(), profile.AvatarUrl);

            if (!System.IO.File.Exists(avatarPhysicalPath))
            {
                // 如果头像文件不存在，返回404
                return NotFound();
            }
            return PhysicalFile(avatarPhysicalPath, contentType);
        }
        
        [HttpPost("avatar/update")]
        public IActionResult UploadAvatar([FromForm] IFormFile avatar, [FromForm] string user)
        {
            var profile = _dbContextSec.UserProfiles.FirstOrDefault(p => p.RealName == user);
            if (profile == null) return NotFound("Profile not found");

            if (avatar != null && avatar.Length > 0)
            {
                var uploadsFolder = Path.Combine("wwwroot", "Avatar");
                Directory.CreateDirectory(uploadsFolder); // 确保目录存在

                var fileExtension = Path.GetExtension(avatar.FileName);
                var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    avatar.CopyTo(fileStream);
                }

                // 更新用户头像URL,保存到数据库
                profile.AvatarUrl = Path.Combine("Avatar", uniqueFileName).Replace("\\", "/");
                _dbContextSec.Update(profile);
                _dbContextSec.SaveChanges();

            var avatarPhysicalPath = Path.Combine(Directory.GetCurrentDirectory(), profile.AvatarUrl);

            if (!System.IO.File.Exists(avatarPhysicalPath))
            {
                // 如果头像文件不存在，返回404
                return NotFound();
            }
                var contentType = "image/jpeg";
                //返回上传后的头像二进制数据
                return PhysicalFile(avatarPhysicalPath, contentType);
            }

            return BadRequest("Invalid avatar file.");
        }
    }
}
