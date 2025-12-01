using Application.DTOs.Menu;
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

public class MenuService(
    IMenuRepository menuRepository,
    IRoleRepository roleRepository,
    IUnitOfWork unitOfWork,
    IMapper mapper,
    ICurrentUserService currentUserService,
    IValidator<CreateMenuDto> createMenuValidator,
    IValidator<UpdateMenuDto> updateMenuValidator,
    ILogger<MenuService> logger
) : IMenuService
{
    /// <summary>
    ///     分页查询菜单
    /// </summary>
    /// <param name="parameters"></param>
    /// <returns></returns>
    public async Task<PagedResult<MenuDto>> GetPagedMenusAsync(MenuQueryParameters parameters)
    {
        // 调用通用分页方法
        var pageDate = await menuRepository.GetPagedAsync(
            parameters.QueryBuild(),
            parameters.Current,
            parameters.Size
        );
        // 返回结果
        return mapper.ToPagedResult<Menu, MenuDto>(pageDate, parameters);
    }

    /// <summary>
    ///     创建菜单
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public async Task<MenuDto> CreateMenuAsync(CreateMenuDto request)
    {
        var validationResult = await createMenuValidator.ValidateAsync(request);
        if (!validationResult.IsValid) throw new ValidationException(validationResult.Errors);
        var menu = mapper.Map<Menu>(request);
        var userId = currentUserService.GetUserId();
        var userName = currentUserService.GetUserName();
        menu.CreateBy = userId;
        menu.CreateName = userName;
        try
        {
            await menuRepository.AddAsync(menu);
            await unitOfWork.SaveChangesAsync();
            logger.LogInformation("菜单创建成功-Id:{MenuId},Name:{Name}, CreatedBy:{CreatedBy}", menu.Id, menu.Name, userId);
            return mapper.Map<MenuDto>(menu);
        }
        catch (Exception e)
        {
            logger.LogError("菜单创建失败-Error:{Error}, CreatedBy:{CreatedBy}", e.Message, userId);
            throw new BusinessException("菜单创建失败", e);
        }
    }

    /// <summary>
    ///     根据id获取菜单
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task<MenuDto> GetMenuByIdAsync(Guid id)
    {
        var menu = await menuRepository.GetByIdAsync(id);
        return menu is null ? throw new NotFoundException("菜单不存在") : mapper.Map<MenuDto>(menu);
    }

    /// <summary>
    ///     获取所有菜单
    /// </summary>
    /// <returns></returns>
    public async Task<List<MenuDto>> GetAllMenusAsync()
    {
        var menus = await menuRepository.GetAllAsync();
        return mapper.Map<List<MenuDto>>(menus);
    }
    
    /// <summary>
    ///     获取所有菜单树形结构
    /// </summary>
    /// <returns></returns>
    public async Task<List<MenuTreeDto>> GetAllMenusTreeAsync()
    {
        var menus = await menuRepository.GetAllAsync();
        return mapper.Map<List<MenuTreeDto>>(menus);
        
      
    }

    /// <summary>
    ///     获取树形菜单
    /// </summary>
    /// <returns></returns>
    public async Task<List<MenuTreeDto>> GetMenuTreeAsync()
    {
        var menus = await menuRepository.GetAllAsync();
        // 构建菜单树的逻辑
        return mapper.Map<List<MenuTreeDto>>(menus);
    }

    /// <summary>
    ///     根据角色id获取菜单列表
    /// </summary>
    /// <param name="roleId"></param>
    /// <returns></returns>
    /// <exception cref="NotFoundException"></exception>
    public async Task<List<MenuDto>> GetMenusByRoleIdAsync(Guid roleId)
    {
        var role = await roleRepository.GetByIdAsync(roleId);
        if (role is null)
            throw new NotFoundException("菜单不存在");
        var menus = await menuRepository.GetMenusByRoleIdAsync(roleId);
        // 构建菜单树的逻辑
        return mapper.Map<List<MenuDto>>(menus);
    }

    public async Task<List<MenuDto>> GetMenuTreeByRoleIdAsync(Guid roleId)
    {
        var role = await roleRepository.GetByIdAsync(roleId);
        if (role is null)
            throw new NotFoundException("菜单不存在");
        var menus = await menuRepository.GetMenusByRoleIdAsync(roleId);
        // 构建菜单树的逻辑
        return mapper.Map<List<MenuDto>>(menus);
    }

    /// <summary>
    ///     更新菜单
    /// </summary>
    /// <param name="id"></param>
    /// <param name="request"></param>
    /// <exception cref="Exceptions.ValidationException"></exception>
    /// <exception cref="NotFoundException"></exception>
    public async Task UpdateMenuAsync(Guid id, UpdateMenuDto request)
    {
        var validationResult = await updateMenuValidator.ValidateAsync(request);
        if (!validationResult.IsValid) throw new ValidationException(validationResult.Errors);
        var menu = await menuRepository.GetByIdAsync(id);
        if (menu is null)
            throw new NotFoundException("菜单不存在");
        mapper.Map(request, menu);
        var userId = currentUserService.GetUserId();
        var userName = currentUserService.GetUserName();
        menu.UpdateBy = userId;
        menu.UpdateName = userName;
        try
        {
            await menuRepository.UpdateAsync(menu);
            await unitOfWork.SaveChangesAsync();
            logger.LogInformation("菜单更新成功-Id:{MenuId},Name:{Name}, ModifiedBy:{ModifiedBy}", menu.Id, menu.Name,
                userId);
        }
        catch (Exception e)
        {
            logger.LogError("菜单更新失败-Error:{Error}, ModifiedBy:{ModifiedBy}", e.Message, userId);
            throw new BusinessException("菜单更新失败", e);
        }
    }

    public async Task DeleteMenuAsync(Guid id)
    {
        var menu = await menuRepository.GetByIdAsync(id);
        if (menu is null)
            throw new NotFoundException("菜单不存在");
        try
        {
            await menuRepository.DeleteAsync(menu);
        }
        catch(InvalidOperationException e)
        {
            throw new BusinessException(e.Message, e);
        }
       
    }
}