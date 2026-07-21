using Application.DTOs;
using Application.QueryParameters;

namespace Application.Interfaces;

/// <summary>
///     带名称和编码的基础资料应用服务接口，在通用基础资料能力之上额外提供轻量下拉选项查询。
/// </summary>
public interface INamedCodeBaseDataService<TDto, in TCreateDto, in TUpdateDto, in TQuery>
    : IBaseDataService<TDto, TCreateDto, TUpdateDto, TQuery>
    where TUpdateDto : IHasId
{
    /// <summary>
    ///     获取全部下拉选项，仅返回主键、名称和编码，不加载明细。
    /// </summary>
    Task<List<NamedCodeOptionDto>> GetOptionsAsync();

    /// <summary>
    ///     按关键词限量搜索轻量选择项，不执行总数统计。
    /// </summary>
    Task<SelectionOptionSearchResultDto> SearchSelectionOptionsAsync(SelectionOptionSearchQueryParameters parameters);

    /// <summary>
    ///     按已选主键集合恢复选择项显示文本。
    /// </summary>
    Task<List<SelectionOptionDto>> ResolveSelectionOptionsAsync(IReadOnlyCollection<Guid> ids);

    /// <summary>
    ///     获取轻量有界选择项；数据超过业务上限时拒绝静默截断。
    /// </summary>
    Task<List<SelectionOptionDto>> GetBoundedSelectionOptionsAsync();
}
