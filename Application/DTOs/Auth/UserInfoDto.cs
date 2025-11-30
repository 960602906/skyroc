namespace Application.DTOs.Auth;

/// <summary>
///     用户信息
/// </summary>
public class UserInfoDto
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = ["B_CODE1", "B_CODE2", "B_CODE3"];
    public List<string> Buttons { get; set; } = ["R_ADMIN"];
}