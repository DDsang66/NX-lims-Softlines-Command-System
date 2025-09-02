namespace NX_lims_Softlines_Command_System.Application.DTO
{
    public record LoginRequest(string Email, string Password);
    public record TokenResponse(string AccessToken, string RefreshToken);

    public record RegisterRequest(string Email,string Password,string NickName);
}
