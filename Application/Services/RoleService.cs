using Application.DTOs.Menu;
using Application.DTOs.Role;
using Application.Exceptions;
using Application.Extensions;
using Application.interfaces;
using Application.QueryParameters;
using AutoMapper;
using Common.Constants;
using Domain.Entities;
using Domain.Interfaces;
using FluentValidation;
using Microsoft.Extensions.Logging;
using ValidationException = Application.Exceptions.ValidationException;

namespace Application.Services;

/// <summary>
///     角色应用服务实现
/// </summary>
public class RoleService(
    IRoleRepository roleRepository,
    IMenuRepository menuRepository,
    IUnitOfWork unitOfWork,
    IMapper mapper,
    ICurrentUserService currentUserService,
    IValidator<CreateRoleDto> createRoleValidator,
    IValidator<UpdateRoleDto> updateRoleValidator,
    ILogger<RoleService> logger) : IRoleService
{
    /// <summary>
    ///     查询角色
    /// </summary>
    /// <param name="parameters"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public async Task<PagedResult<RoleDto>> GetPagedMenusAsync(RoleQueryParameters parameters)
    {
        // 调用通用分页方法
        var pageData = await roleRepository.GetPagedAsync(
            parameters.QueryBuild(),
            parameters.Current,
            parameters.Size
        );
        // 返回结果
        return mapper.ToPagedResult<Role, RoleDto>(pageData, parameters);
    }

    /// <summary>
    ///     添加角色
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public async Task<RoleDto> CreateRoleAsync(CreateRoleDto request)
    {
        var validationResult = await createRoleValidator.ValidateAsync(request);
        if (!validationResult.IsValid) throw new ValidationException(validationResult.Errors);
        var role = mapper.Map<Role>(request);
        var userId = currentUserService.GetUserId();
        role.CreatedBy = userId;
        try
        {
            await roleRepository.AddAsync(role);
            await unitOfWork.SaveChangesAsync();
            logger.LogInformation("角色创建成功-Id:{RoleId},Name:{Name}, CreatedBy:{CreatedBy}", role.Id, role.Name, userId);
            return mapper.Map<RoleDto>(role);
        }
        catch (Exception e)
        {
            logger.LogError("角色创建失败-Name:{Name}, CreatedBy:{CreatedBy}, Error:{Error}", role.Name, userId, e.Message);
            throw new BusinessException("角色创建失败", e);
        }
    }

    /// <summary>
    ///     根据id获取角色
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task<RoleDto> GetRoleByIdAsync(Guid id)
    {
        var role = await roleRepository.GetByIdAsync(id);
        if (role is null) throw new NotFoundException("角色不存在");
        var roleDto = mapper.Map<RoleDto>(role);
        var menus = await menuRepository.GetMenusByRoleIdAsync(role.Id);
        roleDto.Menus = mapper.Map<IEnumerable<MenuDto>>(menus);
        return roleDto;
    }

    /// <summary>
    ///     获取所有角色
    /// </summary>
    /// <returns></returns>
    public async Task<IEnumerable<RoleDto>> GetAllRolesAsync()
    {
        var roles = await roleRepository.GetAllAsync();
        return mapper.Map<IEnumerable<RoleDto>>(roles);
    }

    /// <summary>
    ///     更新角色
    /// </summary>
    /// <param name="id"></param>
    /// <param name="request"></param>
    /// <exception cref="KeyNotFoundException"></exception>
    public async Task UpdateRoleAsync(Guid id, UpdateRoleDto request)
    {
        var validationResult = await updateRoleValidator.ValidateAsync(request);
        if (!validationResult.IsValid) throw new ValidationException(validationResult.Errors);
        var role = await roleRepository.GetByIdAsync(id);
        if (role is null) throw new NotFoundException("角色不存在");
        mapper.Map(request, role);
        var userId = currentUserService.GetUserId();
        role.UpdateBy = userId;
        try
        {
            await roleRepository.UpdateAsync(role);
            await unitOfWork.SaveChangesAsync();
            logger.LogInformation("角色更新成功-Id:{RoleId},Name:{Name}, ModifiedBy:{ModifiedBy}", role.Id, role.Name,
                userId);
        }
        catch (Exception e)
        {
            logger.LogError("角色更新失败-Id:{RoleId},Name:{Name}, ModifiedBy:{ModifiedBy}, Error:{Error}", role.Id,
                role.Name, userId, e.Message);
            throw new BusinessException("角色更新失败", e);
        }
    }

    /// <summary>
    ///     删除角色
    /// </summary>
    /// <param name="id"></param>
    /// <exception cref="KeyNotFoundException"></exception>
    public async Task DeleteRoleAsync(Guid id)
    {
        var role = await roleRepository.GetByIdAsync(id);
        if (role is null) throw new NotFoundException("角色不存在");
        await roleRepository.DeleteAsync(role);
        await unitOfWork.SaveChangesAsync();
    }

    /// <summary>
    ///     角色分配菜单
    /// </summary>
    /// <param name="roleId"></param>
    /// <param name="menuIds"></param>
    /// <exception cref="NotFoundException"></exception>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="BusinessException"></exception>
    public async Task AssignMenusToRoleAsync(Guid roleId, IEnumerable<Guid> menuIds)
    {
        //验证角色id是否存在
        var role = await roleRepository.GetByIdAsync(roleId);
        if (role is null) throw new NotFoundException("角色不存在");
        //获取菜单列表
        var menuIdList = menuIds.ToList();
        var menus = await menuRepository.GetByIdsAsync(menuIdList);
        var menusList = menus.ToList();
        if (menuIdList.Count != menusList.Count)
        {
            var invalidRoles = menuIdList.Except(menusList.Select(x => x.Id));
            throw new ArgumentException($"Roles not found: {string.Join(", ", invalidRoles)}");
        }

        //获取角色当前菜单
        var currentMenuIds = await menuRepository.GetMenuIdsByRoleIdAsync(roleId);
        var currentMenusIdList = currentMenuIds.ToList();
        // 计算差异
        var menusToAdd = menuIdList.Except(currentMenusIdList).ToList();
        var menusToRemove = currentMenusIdList.Except(menuIdList).ToList();
        //开启事务
        await unitOfWork.BeginTransactionAsync();
        try
        {
            //删除多余的菜单
            if (menusToRemove.Count != 0) await roleRepository.DeleteByRoleIdAndMenuIdsAsync(roleId, menusToRemove);
            //添加缺少的菜单
            if (menusToAdd.Count != 0) await roleRepository.AddByRoleIdAndMenuIdsAsync(roleId, menusToAdd);
        }
        catch
        {
            //回滚事务
            await unitOfWork.RollbackTransactionAsync();
            logger.LogError("角色分配失败-RoleId:{RoleId}, MenuIds:{MenuIds}", roleId, string.Join(",", menuIdList));
            throw new BusinessException("分配菜单失败");
        }

        await unitOfWork.SaveChangesAsync();
        await unitOfWork.CommitTransactionAsync();
    }

    public async Task RemoveMenusFromRoleAsync(Guid roleId, IEnumerable<Guid> menuIds)
    {
        await roleRepository.DeleteByRoleIdAndMenuIdsAsync(roleId, menuIds);
    }
}