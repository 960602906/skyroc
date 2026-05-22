using Application.DTOs.Department;
using Application.DTOs.User;
using Application.Exceptions;
using Application.interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Interfaces;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Shared.Constants;
using ValidationException = Application.Exceptions.ValidationException;

namespace Application.Services;

public class DepartmentService(
    IDepartmentRepository departmentRepository,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    ILogger<DepartmentService> logger,
    IMapper mapper,
    ICurrentUserService currentUserService,
    IValidator<CreateDepartmentDto> createDepartmentValidator,
    IValidator<UpdateDepartmentDto> updateDepartmentValidator
    ): IDepartmentService
{
    /// <summary>
    /// 获取部门树
    /// </summary>
    public async Task<List<DepartmentTreeDto>> GetDepartmentTreeAsync()
    {
        try
        {
            var departments = await departmentRepository.GetAllDepartmentsAsync();
            return mapper.Map<List<DepartmentTreeDto>>(departments);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "获取部门树失败");
            throw new BusinessException("获取部门失败");
        }
    }
    /// <summary>
    /// 获取部门详情
    /// </summary>
    public async Task<DepartmentDto> GetByIdAsync(Guid id)
    {
        var result = await departmentRepository.GetByIdAsync(id);
        if (result is null)
        {
            throw new BusinessException("部门id不存在");
        }
        return mapper.Map<DepartmentDto>(result);
    }
    /// <summary>
    /// 创建部门
    /// </summary>
    public async Task<DepartmentDto> CreateAsync(CreateDepartmentDto dto)
    {
        var validationResult = await createDepartmentValidator.ValidateAsync(dto);
        if (!validationResult.IsValid) throw new ValidationException(validationResult.Errors);
        var code = await departmentRepository.ExistsByCodeAsync(dto.Code!);
        if (code)
        {
            throw new BusinessException("部门代码已经存在");
        }
        var department = mapper.Map<Department>(dto);
        var userId = currentUserService.GetUserId();
        var userName = currentUserService.GetUserName();
        department.CreateBy = userId;
        department.CreateName = userName;
        await departmentRepository.AddAsync(department);
        await unitOfWork.SaveChangesAsync();
        return mapper.Map<DepartmentDto>(department);
    }

    public async Task<DepartmentDto> UpdateAsync(Guid id, UpdateDepartmentDto dto)
    {
        var validationResult = await updateDepartmentValidator.ValidateAsync(dto);
        if (!validationResult.IsValid) throw new ValidationException(validationResult.Errors);
        var code = await departmentRepository.ExistsByCodeAsync(dto.Code!, id);
        if (code)
        {
            throw new BusinessException("部门代码已经存在");
        }
        var department = await departmentRepository.GetByIdAsync(id);
        if (department is null) throw new NotFoundException("部门不存在");
        mapper.Map(dto, department);
        var userId = currentUserService.GetUserId();
        var userName = currentUserService.GetUserName();
        department.UpdateBy = userId;
        department.UpdateName  = userName;
        await departmentRepository.UpdateAsync(department);
        await unitOfWork.SaveChangesAsync();
        return  mapper.Map<DepartmentDto>(department);
    }
    /// <summary>
    /// 删除部门
    /// </summary>
    public async Task<bool> DeleteAsync(Guid id)
    {
        var department = await departmentRepository.GetByIdAsync(id);
        if (department is null) throw new BusinessException("部门不存在");
        var isChildren = await departmentRepository.HasChildrenAsync(id);
        if (isChildren) throw new BusinessException("部门下还有子部门，不能删除");
        await departmentRepository.DeleteAsync(id);
        await unitOfWork.SaveChangesAsync();
        logger.LogInformation($"删除部门成功: {department.Name}({department.Code})");
        return true;
    }

    /// <summary>
    /// 批量删除部门
    /// </summary>
    public async Task<bool> BatchDeleteAsync(List<Guid>? ids)
    {
        if (ids is null || ids.Count == 0)
        {
            throw new BusinessException("请选择要删除的部门");
        }
       var  departments =  await departmentRepository.GetByIdsAsync(ids.ToArray());
       if (departments.Count != ids.Count) throw new BusinessException("部分部门不存在呢");
       foreach (var dept in departments)
       {
           if (await departmentRepository.HasChildrenAsync(dept.Id))
           {
               throw new BusinessException($"部门 {dept.Name} 下还有子部门，不能删除");
           }
       }
       await departmentRepository.DeleteRangeAsync(ids.ToArray());
       await unitOfWork.SaveChangesAsync();
       logger.LogInformation($"批量删除部门成功: {ids.Count} 个部门");
       return true;
    }
    
    /// <summary>
    /// 启用/禁用部门
    /// </summary>
    public async Task<DepartmentDto> ToggleStatusAsync(Guid id, Status status)
    {
        var department = await departmentRepository.GetByIdAsync(id);
        if (department is null) throw new BusinessException("部门id 不存在");
        department.Status = status;
        await departmentRepository.UpdateAsync(department);
        await unitOfWork.SaveChangesAsync();
        logger.LogInformation($"部门状态变更成功: {department.Name}({department.Code})={department.Status}");
        return  mapper.Map<DepartmentDto>(department);
    }
    /// <summary>
    /// 获取部门下的用户列表
    /// </summary>
    public async Task<List<UserDto>> GetUsersAsync(Guid departmentId)
    {
        var guids = new List<Guid> { departmentId };
        var users = await userRepository.GetByDepartmentIdsAsync(guids);
        return  mapper.Map<List<UserDto>>(users);
    }
}
