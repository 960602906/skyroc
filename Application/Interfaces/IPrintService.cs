using Application.DTOs.Printing;
using Domain.Entities.Printing;
using Shared.Common;
using Shared.Constants;

namespace Application.Interfaces;

/// <summary>
/// 打印应用服务，维护模板及字段定义，并从既有业务单据生成只读打印快照。
/// </summary>
public interface IPrintService
{
    /// <summary>分页查询打印模板，不返回已删除记录。</summary>
    /// <param name="pageNumber">从 1 开始的页码。</param>
    /// <param name="pageSize">每页记录数，最大 100。</param>
    /// <returns>模板分页结果。</returns>
    Task<PagedResult<PrintTemplateDto>> GetTemplatesAsync(int pageNumber, int pageSize);

    /// <summary>按稳定编码读取模板及字段定义。</summary>
    /// <param name="templateCode">打印模板稳定业务编码。</param>
    /// <returns>模板完整配置。</returns>
    Task<PrintTemplateDto> GetTemplateByCodeAsync(string templateCode);

    /// <summary>新增打印模板和字段定义。</summary>
    /// <param name="dto">模板编码、业务类型、设计 JSON 和字段集合。</param>
    /// <returns>创建后的模板完整配置。</returns>
    Task<PrintTemplateDto> CreateTemplateAsync(CreatePrintTemplateDto dto);

    /// <summary>完整更新打印模板和字段定义，字段集合会替换原配置。</summary>
    /// <param name="dto">包含模板主键的完整替换请求。</param>
    /// <returns>更新后的模板完整配置。</returns>
    Task<PrintTemplateDto> UpdateTemplateAsync(UpdatePrintTemplateDto dto);

    /// <summary>删除打印模板及其字段定义；已删除模板不再提供给业务打印。</summary>
    /// <param name="id">待删除的模板主键。</param>
    /// <returns>删除成功标记。</returns>
    Task<bool> DeleteTemplateAsync(Guid id);

    /// <summary>读取指定业务单据的打印数据，不改变来源单据打印状态。</summary>
    /// <param name="businessType">打印业务单据类型。</param>
    /// <param name="ids">来源单据主键集合，单次最多 100 个且不能重复。</param>
    /// <returns>与输入主键顺序一致的打印数据集合。</returns>
    Task<IReadOnlyList<PrintDocumentDto>> GetDataAsync(PrintBusinessType businessType, IReadOnlyCollection<Guid> ids);

    /// <summary>确认正式打印已完成，并仅更新支持打印状态的订单、入库单或出库单。</summary>
    /// <param name="businessType">打印业务单据类型。</param>
    /// <param name="ids">已完成正式打印的来源单据主键集合。</param>
    Task ConfirmPrintedAsync(PrintBusinessType businessType, IReadOnlyCollection<Guid> ids);
}
