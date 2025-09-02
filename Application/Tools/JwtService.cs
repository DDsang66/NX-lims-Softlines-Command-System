using Microsoft.IdentityModel.Tokens;
using NX_lims_Softlines_Command_System.Application.DTO;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace NX_lims_Softlines_Command_System.Application.Tools
{
    public class JwtService
    {
        private readonly IConfiguration _cfg;
        public JwtService(IConfiguration cfg) => _cfg = cfg;

        public TokenResponse GenerateTokens(string userId)
        {
            var jwt = _cfg.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Secret"]!));
            var cred = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var accessToken = new JwtSecurityToken(
                issuer: jwt["Issuer"],
                audience: jwt["Audience"],
                claims: new[] { new Claim("uid", userId) },
                expires: DateTime.UtcNow.AddMinutes(int.Parse(jwt["ExpireMinutes"]!)),
                signingCredentials: cred);

            var refreshToken = new JwtSecurityToken(
                issuer: jwt["Issuer"],
                audience: jwt["Audience"],
                claims: new[] { new Claim("uid", userId), new Claim("type", "refresh") },
                expires: DateTime.UtcNow.AddHours(int.Parse(jwt["RefreshExpireHours"]!)),
                signingCredentials: cred);

            return new TokenResponse(
                new JwtSecurityTokenHandler().WriteToken(accessToken),
                new JwtSecurityTokenHandler().WriteToken(refreshToken));
        }
    }
}
