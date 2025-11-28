namespace Application.DTOs.User;

/// <summary>
///     修改密码DTO
/// </summary>
public class ChangePasswordDto
{
    /// <summary>
    ///     用户ID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    ///     旧密码
    /// </summary>
    public required string OldPassword { get; set; }

    /// <summary>
    ///     新密码
    /// </summary>
    public required string NewPassword { get; set; }

    /// <summary>
    ///     确认新密码
    /// </summary>
    public required string ConfirmPassword { get; set; }
}