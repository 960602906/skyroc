using Application.DTOs.MenuButton;
using Application.Exceptions;
using Application.interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Interfaces;
using FluentValidation;
using Microsoft.Extensions.Logging;
using ValidationException = Application.Exceptions.ValidationException;

namespace Application.Services;

/// <inheritdoc />
public class MenuButtonService(
    IMenuButtonRepository menuButtonRepository,
    IMenuRepository menuRepository,
    IMapper mapper,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork,
    IValidator<CreateMenuButtonDto> createMenuButtonValidator,
    IValidator<UpdateMenuButtonDto> updateMenuButtonValidator,
    ILogger<MenuButtonService> logger
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

    /// <summary>
    /// 创建菜单按钮
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    /// <exception cref="BusinessException"></exception>
    public async Task<MenuButtonDto> CreateMenuButtonAsync(CreateMenuButtonDto request)
    {
        var validationResult = await createMenuButtonValidator.ValidateAsync(request);
        if (!validationResult.IsValid) throw new ValidationException(validationResult.Errors);
        var menuButton = mapper.Map<MenuButton>(request);
        var userId = currentUserService.GetUserId();
        var userName = currentUserService.GetUserName();
        menuButton.CreateBy = userId;
        menuButton.CreateName = userName;
        try
        {
            await menuButtonRepository.AddAsync(menuButton);
            await unitOfWork.SaveChangesAsync();
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
    public async Task<List<MenuButtonDto>> CreateMenuButtonsAsync(Guid menuId,
        List<CreateMenuButtonDto> requests)
    {
        var requestList = requests.ToList();
        // 严重输入
        if (requestList.Count == 0)
        {
            logger.LogWarning("没有提供任何菜单按钮数据");
            throw new BusinessException("没有提供任何菜单按钮数据");
        }
        // 并行验证所有请求
        var validationTasks = requestList
            .Select((request, index) => new { Request = request, Index = index })
            .Select(async item => new
            {
                item.Index,
                Result = await createMenuButtonValidator.ValidateAsync(item.Request)
            });
        var validationResults = await Task.WhenAll(validationTasks);
        // 收集所有验证错误
        var validationErrors = validationResults
            .Where(vr => !vr.Result.IsValid)
            .SelectMany(vr => vr.Result.Errors.Select(e =>
                new FluentValidation.Results.ValidationFailure($"[索引{vr.Index}] {e.PropertyName}", e.ErrorMessage)))
            .ToList();
        if (validationErrors.Count != 0)
        {
            logger.LogWarning("批量更新菜单按钮验证失败，菜单ID：{MenuId}", menuId);
            throw new ValidationException(validationErrors);
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
            await unitOfWork.SaveChangesAsync();

            logger.LogInformation("批量创建菜单按钮成功，菜单ID：{MenuId}，共创建 {Count} 个按钮",
                menuId, menuButtons.Count);

            return mapper.Map<List<MenuButtonDto>>(menuButtons);
        }
        catch (Exception e)
        {
            logger.LogError(e, "批量创建菜单按钮失败，菜单ID：{MenuId}，错误信息：{Message}", menuId, e.Message);
            throw new BusinessException("批量创建菜单按钮失败", e);
        }
    }

    /// <summary>
    ///     更新菜单按钮
    /// </summary>
    /// <param name="menuId"></param>
    /// <param name="request"></param>
    /// <returns></returns>
    /// <exception cref="BusinessException"></exception>
    public async Task<MenuButtonDto> UpdateMenuButtonAsync(Guid menuId, UpdateMenuButtonDto request)
    {
        var validationResult = await updateMenuButtonValidator.ValidateAsync(request);
        if (!validationResult.IsValid) throw new ValidationException(validationResult.Errors);
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
            await unitOfWork.SaveChangesAsync();

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

    /// <summary>
    ///     删除菜单按钮
    /// </summary>
    /// <param name="id"></param>
    /// <exception cref="NotFoundException"></exception>
    /// <exception cref="BusinessException"></exception>
    public async Task DeleteMenuButtonAsync(Guid id)
    {
        var menuButton = await menuButtonRepository.GetByIdAsync(id);
        if (menuButton is null)
            throw new NotFoundException("菜单按钮不存在");
        try
        {
            await menuButtonRepository.DeleteAsync(menuButton);
            await unitOfWork.SaveChangesAsync();
        }
        catch (InvalidOperationException e)
        {
            throw new BusinessException(e.Message, e);
        }
    }

    /// <summary>
    ///     批量替换菜单按钮
    /// </summary>
    /// <param name="menuId"></param>
    /// <param name="requests"></param>
    /// <returns></returns>
    /// <exception cref="BusinessException"></exception>
    public async Task<List<MenuButtonDto>> ReplaceMenuButtonsAsync(Guid menuId, List<CreateMenuButtonDto> requests)
    {
        var requestList = requests.ToList();
        // 验证菜单是否存在
        var menu = await menuRepository.GetByIdAsync(menuId);
        if (menu is null)
        {
            logger.LogWarning("覆盖菜单按钮失败：菜单不存在，菜单ID：{MenuId}", menuId);
            throw new BusinessException($"菜单不存在，菜单ID：{menuId}");
        }

        var userId = currentUserService.GetUserId();
        var userName = currentUserService.GetUserName();
        // 如果请求列表不为空，验证是否存在重复编码
        if (requestList.Count != 0)
        {
            var codes = requestList.Select(x => x.Code).ToList();
            var duplicateCodes = codes.GroupBy(x => x)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateCodes.Count != 0)
            {
                logger.LogWarning("覆盖菜单按钮失败：请求中存在重复的按钮编码，菜单ID：{MenuId}，重复编码：{Codes}",
                    menuId, string.Join(", ", duplicateCodes));
                throw new BusinessException($"请求中存在重复的按钮编码：{string.Join(", ", duplicateCodes)}");
            }
        }
        // 并行验证所有请求
        var validationTasks = requestList
            .Select((request, index) => new { Request = request, Index = index })
            .Select(async item => new
            {
                item.Index,
                Result = await createMenuButtonValidator.ValidateAsync(item.Request)
            });
        var validationResults = await Task.WhenAll(validationTasks);
        // 收集所有验证错误
        var validationErrors = validationResults
            .Where(vr => !vr.Result.IsValid)
            .SelectMany(vr => vr.Result.Errors.Select(e =>
                new FluentValidation.Results.ValidationFailure($"[索引{vr.Index}] {e.PropertyName}", e.ErrorMessage)))
            .ToList();
        if (validationErrors.Count != 0)
        {
            logger.LogWarning("批量更新菜单按钮验证失败，菜单ID：{MenuId}", menuId);
            throw new ValidationException(validationErrors);
        }

        try
        {
            // 走 ExecuteInTransactionAsync，兼容 EnableRetryOnFailure 的执行策略
            return await unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                // 1. 获取该菜单下的所有现有按钮
                var existingButtons = await menuButtonRepository.GetByMenuIdAsync(menuId);
                // 2. 删除所有现有按钮
                var menuButtons = existingButtons.ToList();
                if (menuButtons.Count != 0)
                {
                    await menuButtonRepository.DeleteRangeAsync(menuButtons);
                    logger.LogInformation("已删除菜单 {MenuId} 下的 {Count} 个按钮", menuId, menuButtons.Count);
                }

                // 3. 创建新的按钮
                var newButtons = requestList.Select(request =>
                {
                    var menuButton = mapper.Map<MenuButton>(request);
                    menuButton.MenuId = menuId;
                    menuButton.CreateBy = userId;
                    menuButton.CreateName = userName;
                    return menuButton;
                }).ToList();
                await menuButtonRepository.AddRangeAsync(newButtons);
                logger.LogInformation("已为菜单 {MenuId} 创建 {Count} 个新按钮", menuId, newButtons.Count);
                return mapper.Map<List<MenuButtonDto>>(newButtons);
            });
        }
        catch (BusinessException)
        {
            throw;
        }
        catch (Exception e)
        {
            logger.LogError(e, "批量替换菜单按钮失败: 菜单ID：{MenuId}，错误信息：{Message}", menuId, e.Message);
            throw new BusinessException("批量替换菜单按钮失败");
        }
    }
}
