using Shared.Constants;

namespace Domain.Entities;

/// <summary>
///  部门实体
/// </summary>
public class Department : BaseEntity
{
    /// <summary>
    /// 部门名称
    /// </summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// 部门代码
    /// </summary>
    public string Code { get; set; } = string.Empty;
    /// <summary>
    /// 父级部门ID
    /// </summary>
    public Guid? ParentId { get; set; }
    /// <summary>
    /// 负责人ID
    /// </summary>
    public Guid? LeaderId { get; set; }

    /// <summary>
    /// 负责人ID
    /// </summary>
    public string? LeaderName { get; set; }
    /// <summary>
    /// 联系电话
    /// </summary>
    public string? Phone { get; set; }
    /// <summary>
    /// 邮箱
    /// </summary>
    public string? Email { get; set; }
    /// <summary>
    /// 排序
    /// </summary>
    public int Sort { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
    // 导航属性
    /// <summary>
    /// 部门负责人导航属性。
    /// </summary>
    public virtual User? Leader { get; set; }
    /// <summary>
    /// 部门下属用户集合。
    /// </summary>
    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
