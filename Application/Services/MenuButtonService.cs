using Application.DTOs.MenuButton;
using Application.Exceptions;
using Application.interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Application.Services;

public class MenuButtonService(
    IMenuButtonRepository menuButtonRepository,
    IMenuRepository menuRepository,
    IMapper mapper,
    ICurrentUserService currentUserService,
    ILogger<MenuService> logger
) : IMenuButtonService
{
    /// <summary>
    ///     根据id获取菜单按钮
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public async Task<MenuButtonDto> GetMenuButtonAsync(Guid id)
    {
        var menuButton = await menuButtonRepository.GetByIdAsync(id);
        return menuButton is null ? throw new NotFoundException("菜单按钮不存在") : mapper.Map<MenuButtonDto>(menuButton);
    }

    public async Task<MenuButtonDto> CreateMenuButtonAsync(CreateMenuButtonDto request)
    {
        var menuButton = mapper.Map<MenuButton>(request);
        var userId = currentUserService.GetUserId();
        var userName = currentUserService.GetUserName();
        menuButton.CreateBy = userId;
        menuButton.CreateName = userName;
        try
        {
            await menuButtonRepository.AddAsync(menuButton);
            logger.LogInformation("菜单按钮创建成功，菜单按钮ID：{MenuButtonId}", menuButton.Id);
            return mapper.Map<MenuButtonDto>(menuButton);
        }
        catch (Exception e)
        {
            logger.LogError("菜单按钮创建失败: {@error}", e.Message);
            throw new BusinessException("菜单按钮创建失败", e);
        }
    }

    /// <summary>
    ///     批量创建菜单按钮
    /// </summary>
    /// <param name="menuId"></param>
    /// <param name="requests"></param>
    /// <returns></returns>
    /// <exception cref="BusinessException"></exception>
    public async Task<IEnumerable<MenuButtonDto>> CreateMenuButtonsAsync(Guid menuId,
        IEnumerable<CreateMenuButtonDto> requests)
    {
        var requestList = requests.ToList();
        // 严重输入
        if (requestList.Count != 0)
        {
            logger.LogWarning("没有提供任何菜单按钮数据");
            throw new BusinessException("没有提供任何菜单按钮数据");
        }

        // 验证菜单是否存在
        var menu = await menuRepository.GetByIdAsync(menuId);
        if (menu is null)
        {
            logger.LogWarning("批量创建菜单按钮失败：菜单不存在，菜单ID：{MenuId}", menuId);
            throw new BusinessException($"菜单不存在，菜单ID：{menuId}");
        }

        var userId = currentUserService.GetUserId();
        var userName = currentUserService.GetUserName();
        // 验证请求中是否存在重复的按钮编码
        var codes = requestList.Select(x => x.Code).ToList();
        var duplicateCodes = codes.GroupBy(x => x)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();
        if (duplicateCodes.Count != 0)
        {
            logger.LogWarning("批量创建菜单按钮失败：请求中存在重复的按钮编码，菜单ID：{MenuId}，重复编码：{Codes}",
                menuId, string.Join(", ", duplicateCodes));
            throw new BusinessException($"请求中存在重复的按钮编码：{string.Join(", ", duplicateCodes)}");
        }

        // 批量获取该菜单下已存在的按钮
        var existingButtons = await menuButtonRepository.GetByMenuIdAsync(menuId);
        var existingCodes = existingButtons.Select(b => b.Code).ToHashSet();
        // 检查是否有重复的按钮编码
        var duplicateInDb = requestList
            .Where(r => r.Code != null && existingCodes.Contains(r.Code))
            .Select(r => r.Code)
            .ToList();
        if (duplicateInDb.Count != 0)
        {
            logger.LogWarning("批量创建菜单按钮失败：按钮编码已存在，菜单ID：{MenuId}，编码：{Codes}",
                menuId, string.Join(", ", duplicateInDb));
            throw new BusinessException($"以下按钮编码已存在：{string.Join(", ", duplicateInDb)}");
        }

        // 映射实体并设置创建信息
        var menuButtons = requestList.Select(request =>
        {
            var menuButton = mapper.Map<MenuButton>(request);
            menuButton.MenuId = menuId;
            menuButton.CreateBy = userId;
            menuButton.CreateName = userName;
            return menuButton;
        }).ToList();
        try
        {
            await menuButtonRepository.AddRangeAsync(menuButtons);

            logger.LogInformation("批量创建菜单按钮成功，菜单ID：{MenuId}，共创建 {Count} 个按钮",
                menuId, menuButtons.Count);

            return mapper.Map<IEnumerable<MenuButtonDto>>(menuButtons);
        }
        catch (Exception e)
        {
            logger.LogError(e, "批量创建菜单按钮失败，菜单ID：{MenuId}，错误信息：{Message}", menuId, e.Message);
            throw new BusinessException("批量创建菜单按钮失败", e);
        }
    }

    public async Task<MenuButtonDto> UpdateMenuButtonAsync(Guid menuId, UpdateMenuButtonDto request)
    {
        // 验证菜单是否存在
        var menu = await menuRepository.GetByIdAsync(menuId);
        if (menu is null)
        {
            logger.LogWarning("更新菜单按钮失败：菜单不存在，菜单ID：{MenuId}", menuId);
            throw new BusinessException($"菜单不存在，菜单ID：{menuId}");
        }

        // 获取要更新的按钮
        var existingButton = await menuButtonRepository.GetByIdAsync(request.Id);
        if (existingButton is null)
        {
            logger.LogWarning("更新菜单按钮失败：按钮不存在，按钮ID：{ButtonId}", request.Id);
            throw new BusinessException($"按钮不存在，按钮ID：{request.Id}");
        }

        // 验证按钮是否属于该菜单
        if (existingButton.MenuId != menuId)
        {
            logger.LogWarning("更新菜单按钮失败：按钮不属于该菜单，按钮ID：{ButtonId}，菜单ID：{MenuId}",
                request.Id, menuId);
            throw new BusinessException("按钮不属于该菜单");
        }

        // 如果修改了编码，验证新编码是否已存在
        if (existingButton.Code != request.Code)
        {
            var codeExists =
                await menuButtonRepository.FirstFindAsync(x => x.MenuId == menuId && x.Code == request.Code);
            if (codeExists != null && codeExists.Id != request.Id)
            {
                logger.LogWarning("更新菜单按钮失败：按钮编码已存在，菜单ID：{MenuId}，编码：{Code}",
                    menuId, request.Code);
                throw new BusinessException($"按钮编码 '{request.Code}' 已存在");
            }
        }

        var userId = currentUserService.GetUserId();
        var userName = currentUserService.GetUserName();
        // 更新按钮属性
        mapper.Map(request, existingButton);
        existingButton.UpdateBy = userId;
        existingButton.UpdateName = userName;
        try
        {
            await menuButtonRepository.UpdateAsync(existingButton);

            logger.LogInformation("更新菜单按钮成功，按钮ID：{ButtonId}，菜单ID：{MenuId}",
                existingButton.Id, menuId);

            return mapper.Map<MenuButtonDto>(existingButton);
        }
        catch (Exception e)
        {
            logger.LogError(e, "更新菜单按钮失败，按钮ID：{ButtonId}，错误信息：{Message}",
                request.Id, e.Message);
            throw new BusinessException("更新菜单按钮失败", e);
        }
    }

    public async Task<IEnumerable<MenuButtonDto>> UpdateMenuButtonsAsync(Guid menuId,
        IEnumerable<UpdateMenuButtonDto> menuButtons)
    {
        throw new NotImplementedException();
    }

    public async Task DeleteMenuButtonAsync(Guid id)
    {
        throw new NotImplementedException();
    }
}