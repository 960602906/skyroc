using Common.Constants;

namespace Application.DTOs.User;

/// <summary>
///     用户DTO - 用于数据传输
/// </summary>
public class UserDto : BaseDto
{
    /// <summary>
    ///     用户名
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    ///     性别
    /// </summary>
    public GenderType? UserGender { get; set; }

    /// <summary>
    ///     昵称
    /// </summary>
    public string? NickName { get; set; }

    /// <summary>
    ///     电话
    /// </summary>
    public string? UserPhone { get; set; }

    /// <summary>
    ///     邮箱
    /// </summary>
    public string? UserEmail { get; set; }
}