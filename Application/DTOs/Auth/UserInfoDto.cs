namespace Application.DTOs.Auth;

/// <summary>
///     用户信息
/// </summary>
public class UserInfoDto
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = [];
    public List<string> Buttons { get; set; } = [];
}