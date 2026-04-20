using Application.DTOs.Department;
using Application.DTOs.User;
using Shared.Common;
using Shared.Constants;

namespace Application.interfaces;

public interface  IDepartmentService
{
    /// <summary>
    /// 获取部门树
    /// </summary>
    Task<List<DepartmentTreeDto>> GetDepartmentTreeAsync();
    /// <summary>
    /// 获取部门详情
    /// </summary>
    Task<DepartmentDto> GetByIdAsync(Guid id);
    /// <summary>
    /// 创建部门
    /// </summary>
    Task<DepartmentDto> CreateAsync(CreateDepartmentDto dto);
    /// <summary>
    /// 更新部门
    /// </summary>
    Task<DepartmentDto> UpdateAsync(Guid id, UpdateDepartmentDto dto);
    /// <summary>
    /// 删除部门
    /// </summary>
    Task<bool> DeleteAsync(Guid id);
    /// <summary>
    /// 批量删除部门
    /// </summary>
    Task<bool> BatchDeleteAsync(List<Guid> ids);
    /// <summary>
    /// 启用/禁用部门
    /// </summary>
    Task<DepartmentDto> ToggleStatusAsync(Guid id, Status status);
    /// <summary>
    /// 获取部门下的用户列表
    /// </summary>
    Task<List<UserDto>> GetUsersAsync(Guid departmentId);
  
   
}