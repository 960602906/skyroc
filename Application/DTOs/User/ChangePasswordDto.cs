namespace Application.DTOs.User;

/// <summary>
///     修改密码DTO
/// </summary>
public class ChangePasswordDto
{
    /// <summary>
    ///     旧密码
    /// </summary>
    public required string OldPassword { get; set; }

    /// <summary>
    ///     新密码
    /// </summary>
    public required string NewPassword { get; set; }
}
