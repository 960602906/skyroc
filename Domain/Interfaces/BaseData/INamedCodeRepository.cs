using Domain.Entities;
using Domain.Entities.Customers;
using Domain.Entities.Goods;
using Domain.Entities.Pricing;
using Domain.Entities.Purchases;
using Domain.Entities.Storage;
using Domain.ReadModels.BaseData;
using GoodsEntity = Domain.Entities.Goods.Goods;

namespace Domain.Interfaces;

/// <summary>
///     带名称和编码的基础资料仓储接口。
/// </summary>
public interface INamedCodeRepository<TEntity> : IRepository<TEntity>
    where TEntity : BaseEntity
{
    /// <summary>
    ///     按编码判断是否存在，可排除当前记录。
    /// </summary>
    Task<bool> ExistsByCodeAsync(string code, Guid? excludeId = null);

    /// <summary>
    ///     按名称判断是否存在，可排除当前记录。
    /// </summary>
    Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null);

    /// <summary>
    ///     按多个 ID 获取基础资料。
    /// </summary>
    Task<List<TEntity>> GetByIdsAsync(IEnumerable<Guid> ids);

    /// <summary>
    ///     获取全部下拉选项，仅在数据库侧投影主键、名称和编码，不加载明细导航属性。
    /// </summary>
    Task<List<NamedCodeOption>> GetOptionsAsync();

    /// <summary>
    ///     按名称或编码执行不区分大小写的限量搜索，并仅投影选择项字段。
    /// </summary>
    /// <param name="keyword">名称或编码关键词；为空时返回确定排序的首批数据。</param>
    /// <param name="take">数据库实际读取数量，调用方可多取一条判断是否还有结果。</param>
    Task<List<SelectionOption>> SearchSelectionOptionsAsync(string? keyword, int take);

    /// <summary>
    ///     按主键集合解析已选记录的显示文本。
    /// </summary>
    /// <param name="ids">已去重的业务主键集合。</param>
    Task<List<SelectionOption>> ResolveSelectionOptionsAsync(IReadOnlyCollection<Guid> ids);

    /// <summary>
    ///     按确定顺序读取有限数量的轻量选择项，供明确有界的数据源使用。
    /// </summary>
    /// <param name="take">数据库读取上限；调用方可多取一条检测越界。</param>
    Task<List<SelectionOption>> GetBoundedSelectionOptionsAsync(int take);
}

