using Application.DTOs.User;
using Application.QueryParameters;
using Common.Constants;

namespace Application.interfaces;

/// <summary>
///     用户应用服务接口
/// </summary>
public interface IUserService
{
    /// <summary>
    ///     分页查询菜单
    /// </summary>
    /// <param name="parameters"></param>
    /// <returns></returns>
    Task<PagedResult<UserDto>> GetPagedMenusAsync(UserQueryParameters parameters);

    /// <summary>
    ///     创建用户
    /// </summary>
    Task<UserDto> CreateUserAsync(CreateUserDto request);

    /// <summary>
    ///     根据 ID 获取用户
    /// </summary>
    Task<UserDto> GetUserByIdAsync(Guid id);

    /// <summary>
    ///     根据邮箱获取用户
    /// </summary>
    Task<UserDto?> GetUserByEmailAsync(string email);

    /// <summary>
    ///     获取所有用户
    /// </summary>
    Task<IEnumerable<UserDto>> GetAllUsersAsync();

    /// <summary>
    ///     更新用户
    /// </summary>
    Task UpdateUserAsync(Guid id, UpdateUserDto request);

    /// <summary>
    ///     删除用户
    /// </summary>
    Task DeleteUserAsync(Guid id);

    /// <summary>
    ///     批量删除用户
    /// </summary>
    /// <param name="ids"></param>
    /// <returns></returns>
    Task DeleteUsersAsync(List<Guid> ids);

    /// <summary>
    ///     给用户分配角色
    /// </summary>
    Task AssignRolesToUserAsync(Guid userId, IEnumerable<Guid> roleIds);

    /// <summary>
    ///     移除用户的角色
    /// </summary>
    Task RemoveRolesFromUserAsync(Guid userId, IEnumerable<Guid> roleIds);

    /// <summary>
    ///     更新用户密码
    /// </summary>
    Task UpdatePasswordAsync(Guid id, ChangePasswordDto request);

    /// <summary>
    ///     禁用用户
    /// </summary>
    Task DeactivateUserAsync(Guid id);
}