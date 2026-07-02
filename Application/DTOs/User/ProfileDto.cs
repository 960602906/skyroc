using Shared.Constants;

namespace Application.DTOs.User;

/// <summary>
///     当前用户个人资料
/// </summary>
public class ProfileDto
{
    public Guid Id { get; set; }

    public required string Username { get; set; }

    public required string NickName { get; set; }

    public GenderType Gender { get; set; }

    public string? Phone { get; set; }

    public required string Email { get; set; }
}
