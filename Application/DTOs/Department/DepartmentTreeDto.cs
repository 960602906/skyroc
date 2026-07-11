namespace Application.DTOs.Department;

/// <summary>
///  部门管理树形结构
/// </summary>
public class DepartmentTreeDto : DepartmentDto
{
    /// <summary>
    ///     子菜单集合
    /// </summary>
    public List<DepartmentTreeDto>? Children { get; set; }
}