using Application.DTOs.User;
using Application.Exceptions;
using Application.Extensions;
using Application.interfaces;
using Application.QueryParameters;
using AutoMapper;
using Common.Constants;
using Common.Utils;
using Domain.Entities;
using Domain.Interfaces;
using FluentValidation;
using Microsoft.Extensions.Logging;
using ValidationException = Application.Exceptions.ValidationException;

namespace Application.Services;

/// <summary>
///     用户应用服务实现
/// </summary>
public class UserService(
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    IUnitOfWork unitOfWork,
    IMapper mapper,
    ICurrentUserService currentUserService,
    IValidator<CreateUserDto> createUserValidator,
    IValidator<UpdateUserDto> updateUserValidator,
    ILogger<UserService> logger) : IUserService
{
    public async Task<PagedResult<UserDto>> GetPagedMenusAsync(UserQueryParameters parameters)
    {
        // 调用通用分页方法
        var pageData = await userRepository.GetPagedAsync(
            parameters.QueryBuild(),
            parameters.Current,
            parameters.Size
        );
        // 返回结果
        return mapper.ToPagedResult<User, UserDto>(pageData, parameters);
    }

    /// <summary>
    ///     创建用户
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public async Task<UserDto> CreateUserAsync(CreateUserDto request)
    {
        var validationResult = await createUserValidator.ValidateAsync(request);
        if (!validationResult.IsValid) throw new ValidationException(validationResult.Errors);
        var user = mapper.Map<User>(request);
        if (request.Password != null) user.PasswordHash = PasswordHasher.Hash(request.Password);
        var userId = currentUserService.GetUserId();
        user.CreatedBy = userId;
        try
        {
            await userRepository.AddAsync(user);
            await unitOfWork.SaveChangesAsync();
            logger.LogInformation("用户创建成功: {@user}", user);
            // 重新查询以包含角色信息
            return mapper.Map<UserDto>(user);
        }
        catch (Exception e)
        {
            logger.LogError("用户创建失败: {@error}", e.Message);
            throw new BusinessException("用户创建失败", e);
        }
    }

    /// <summary>
    ///     根据id获取用户
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public async Task<UserDto> GetUserByIdAsync(Guid id)
    {
        var user = await userRepository.GetByIdAsync(id);
        return user is null ? throw new NotFoundException("用户不存在") : mapper.Map<UserDto>(user);
    }

    /// <summary>
    ///     根据邮箱获取用户
    /// </summary>
    /// <param name="email"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public async Task<UserDto?> GetUserByEmailAsync(string email)
    {
        var user = await userRepository.GetByConditionAsync(x => x.Email == email);
        return user is null ? throw new NotFoundException("用户不存在") : mapper.Map<UserDto?>(user);
    }

    /// <summary>
    ///     获取所有用户
    /// </summary>
    /// <returns></returns>
    public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
    {
        var users = await userRepository.GetAllAsync();
        return mapper.Map<IEnumerable<UserDto>>(users);
    }

    /// <summary>
    ///     更新用户
    /// </summary>
    /// <param name="id"></param>
    /// <param name="request"></param>
    /// <exception cref="KeyNotFoundException"></exception>
    public async Task UpdateUserAsync(Guid id, UpdateUserDto request)
    {
        var validationResult = await updateUserValidator.ValidateAsync(request);
        if (!validationResult.IsValid) throw new ValidationException(validationResult.Errors);
        var user = await userRepository.GetByIdAsync(id);
        if (user is null) throw new NotFoundException("用户不存在");
        mapper.Map(request, user);
        var userId = currentUserService.GetUserId();
        user.UpdateBy = userId;
        try
        {
            await userRepository.UpdateAsync(user);
            await unitOfWork.SaveChangesAsync();
            logger.LogInformation("用户更新成功-Id:{UserId},Email:{Email}, ModifiedBy:{ModifiedBy}", user.Id, user.Email,
                userId);
        }
        catch (Exception e)
        {
            logger.LogError("用户更新失败-Id:{UserId},Email:{Email}, ModifiedBy:{ModifiedBy}, Error:{Error}", user.Id,
                user.Email, userId, e.Message);
            throw new BusinessException("用户更新失败", e);
        }
    }

    /// <summary>
    ///     删除用户
    /// </summary>
    /// <param name="id"></param>
    public async Task DeleteUserAsync(Guid id)
    {
        // 可以先检查用户是否存在
        var exists = await userRepository.ExistsAsync(x => x.Id == id);
        if (!exists) throw new NotFoundException("用户不存在");
        await userRepository.DeleteAsync(id);
        await unitOfWork.SaveChangesAsync();
    }

    /// <summary>
    ///     给用户分配角色
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="roleIds"></param>
    /// <exception cref="KeyNotFoundException"></exception>
    public async Task AssignRolesToUserAsync(Guid userId, IEnumerable<Guid> roleIds)
    {
        //查看当前用户是否存在
        var user = await userRepository.GetByIdAsync(userId);
        if (user is null) throw new NotFoundException("用户不存在");
        // 获取角色列表
        var roleIdList = roleIds.ToList();
        var roles = await roleRepository.GetByIdsAsync(roleIdList);
        var roleList = roles.ToList();
        if (roleIdList.Count != roleList.Count)
        {
            var invalidRoles = roleIdList.Except(roleList.Select(x => x.Id));
            throw new ArgumentException($"Roles not found: {string.Join(", ", invalidRoles)}");
        }

        // 获取用户当前角色
        var currentRoleIds = await roleRepository.GetRoleIdsByUserIdAsync(userId);
        // 计算差异
        var enumerable = currentRoleIds.ToList();
        var rolesToAdd = roleIdList.Except(enumerable).ToList();
        var rolesToRemove = enumerable.Except(roleIdList).ToList();
        // 开始事务处理
        await unitOfWork.BeginTransactionAsync();
        try
        {
            //删除多余角色
            if (rolesToRemove.Count != 0) await userRepository.DeleteByUserIdAndRoleIdsAsync(userId, rolesToRemove);
            //添加新角色
            if (rolesToAdd.Count != 0) await userRepository.AddByUserIdAndRoleIdsAsync(userId, rolesToAdd);
        }
        catch
        {
            // 回滚事务
            await unitOfWork.RollbackTransactionAsync();
            logger.LogError("分配角色失败-UserId:{UserId}, RoleIds:{RoleIds}", userId, string.Join(",", roleIdList));
            throw new BusinessException("分配角色失败");
        }

        await unitOfWork.SaveChangesAsync();
        await unitOfWork.CommitTransactionAsync();
    }

    /// <summary>
    ///     移除用户角色
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="roleIds"></param>
    public async Task RemoveRolesFromUserAsync(Guid userId, IEnumerable<Guid> roleIds)
    {
        await userRepository.DeleteByUserIdAndRoleIdsAsync(userId, roleIds);
    }

    public async Task UpdatePasswordAsync(Guid id, ChangePasswordDto request)
    {
        var user = await userRepository.GetByIdAsync(id);
        if (user is null) throw new NotFoundException("用户不存在");
        if (request.NewPassword != request.ConfirmPassword)
            throw new ArgumentException("New password and confirm password do not match");
        mapper.Map(request, user);
        await userRepository.UpdateAsync(user);
        await unitOfWork.SaveChangesAsync();
    }

    public async Task DeactivateUserAsync(Guid id)
    {
        var user = await userRepository.GetByIdAsync(id);
        if (user is null) throw new NotFoundException("用户不存在");
        user.Status = Status.Disable;
        await userRepository.UpdateAsync(user);
        await unitOfWork.SaveChangesAsync();
    }
}