using System.ComponentModel.DataAnnotations;
using Shared.Constants;

namespace Application.QueryParameters;

/// <summary>
///     已选选择项批量解析参数。
/// </summary>
public class SelectionOptionResolveQueryParameters
{
    /// <summary>
    ///     需要恢复显示文本的业务主键集合，单次最多 100 个。
    /// </summary>
    [MaxLength(SelectionOptionConstants.MaxResolveCount)]
    public List<Guid> Ids { get; set; } = [];
}
