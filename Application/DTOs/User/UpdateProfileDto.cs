using Shared.Constants;

namespace Application.DTOs.User;

/// <summary>
///     更新当前用户个人资料
/// </summary>
public class UpdateProfileDto
{
    public required string NickName { get; set; }

    public GenderType Gender { get; set; }

    public string? Phone { get; set; }

    public required string Email { get; set; }
}
