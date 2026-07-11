using Domain.Entities.Printing;

namespace Domain.Interfaces;

/// <summary>
/// 打印模板仓储接口，负责加载模板设计 JSON、字段集合及编码唯一性校验。
/// </summary>
public interface IPrintTemplateRepository : IRepository<PrintTemplate>
{
    /// <summary>按主键读取模板及字段集合。</summary>
    /// <param name="id">打印模板主键。</param>
    /// <returns>模板不存在时返回 <c>null</c>。</returns>
    Task<PrintTemplate?> GetWithFieldsAsync(Guid id);

    /// <summary>按稳定业务编码读取已配置模板及字段集合。</summary>
    /// <param name="templateCode">待查找的模板编码。</param>
    /// <returns>模板不存在时返回 <c>null</c>。</returns>
    Task<PrintTemplate?> GetByCodeAsync(string templateCode);

    /// <summary>检查模板编码是否已被其他模板占用。</summary>
    /// <param name="templateCode">待校验的模板编码。</param>
    /// <param name="excludeId">更新时需要排除的模板主键。</param>
    /// <returns>编码已存在时返回 <c>true</c>。</returns>
    Task<bool> ExistsTemplateCodeAsync(string templateCode, Guid? excludeId = null);

    /// <summary>分页读取模板及其字段定义。</summary>
    /// <param name="pageNumber">从 1 开始的页码。</param>
    /// <param name="pageSize">每页模板数量。</param>
    /// <returns>模板完整聚合和总记录数。</returns>
    Task<(IReadOnlyList<PrintTemplate> Data, int Total)> GetPagedWithFieldsAsync(int pageNumber, int pageSize);

    /// <summary>
    /// 物理删除指定模板字段行。
    /// 更新模板时必须先调用并落库，再插入替换行，避免 PostgreSQL 在相同 FieldKey/DisplayOrder 上先插后删触发唯一约束。
    /// </summary>
    /// <param name="fields">待删除的模板字段实体集合。</param>
    Task RemoveFieldsAsync(IEnumerable<PrintTemplateField> fields);

    /// <summary>批量新增模板字段行，供更新模板时在删除落库后插入替换字段。</summary>
    /// <param name="fields">待插入的模板字段实体集合。</param>
    Task AddFieldsAsync(IEnumerable<PrintTemplateField> fields);
}
