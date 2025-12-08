using Domain.Entities;

namespace Domain.Interfaces;

public interface IDepartmentRepository: IRepository<Department>
{
    /// <summary>
    /// 获取部门树（所有部门层级结构）
    /// </summary>
    Task<List<Department>> GetAllDepartmentsAsync();
    /// <summary>
    /// 获取子部门列表（包括所有子孙部门）
    /// </summary>
    Task<List<Department>> GetDescendantsAsync(Guid departmentId);
    /// <summary>
    /// 获取直接子部门
    /// </summary>
    Task<List<Department>> GetChildrenAsync(Guid parentId);
    /// <summary>
    /// 根据部门代码获取部门
    /// </summary>
    Task<Department?> GetByCodeAsync(string code);
    /// <summary>
    /// 检查部门代码是否存在
    /// </summary>
    Task<bool> ExistsByCodeAsync(string code, Guid? excludeId = null);
    /// <summary>
    /// 检查部门名称是否存在（同一父级下）
    /// </summary>
    Task<bool> ExistsByNameAsync(string name, Guid? parentId, Guid? excludeId = null);
    /// <summary>
    /// 获取部门及其所有父级部门
    /// </summary>
    Task<List<Department>> GetAncestorsAsync(Guid departmentId);
    /// <summary>
    /// 检查部门是否有子部门
    /// </summary>
    Task<bool> HasChildrenAsync(Guid departmentId);
   
    /// <summary>
    /// 获取部门下的所有用户数量
    /// </summary>
    Task<int> GetUserCountAsync(Guid departmentId, bool includeDescendants = false);
    /// <summary>
    /// 批量获取部门
    /// </summary>
    Task<List<Department>> GetByIdsAsync(params Guid[] ids);
}