using Application.DTOs;

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
}
