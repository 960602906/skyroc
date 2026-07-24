using Application.DTOs.AfterSales;
using Application.QueryParameters.AfterSales;
using Shared.Constants;

namespace Application.Interfaces;

/// <summary>
/// 售后应用服务，负责草稿维护、审核状态机和商品处理结果编排。
/// </summary>
public interface IAfterSaleService
{
    /// <summary>按业务条件分页查询售后单列表摘要。</summary>
    Task<PagedResult<AfterSaleListItemDto>> GetPagedAsync(AfterSaleQueryParameters parameters);

    /// <summary>读取售后单商品明细和完整审核轨迹。</summary>
    Task<AfterSaleDto> GetByIdAsync(Guid id);

    /// <summary>根据售后单号读取售后单商品明细和完整审核轨迹。</summary>
    /// <exception cref="Application.Exceptions.BusinessException">售后单号不存在时抛出。</exception>
    Task<AfterSaleDto> GetByAfterSaleNoAsync(string afterSaleNo);

    /// <summary>创建待提交售后单并固化订单、客户、商品、单位和价格快照。</summary>
    Task<AfterSaleDto> CreateAsync(CreateAfterSaleDto dto);

    /// <summary>替换待提交售后单的可编辑信息和完整商品行集合。</summary>
    Task<AfterSaleDto> UpdateAsync(UpdateAfterSaleDto dto);

    /// <summary>删除从未提交过的售后草稿及其商品行。</summary>
    Task<bool> DeleteAsync(Guid id);

    /// <summary>将首次创建的售后草稿提交审核。</summary>
    Task<AfterSaleDto> SubmitAsync(Guid id, string? remark);

    /// <summary>审核通过待审核售后，退货退款商品幂等生成取货任务并进入实物处理阶段。</summary>
    Task<AfterSaleDto> ApproveAsync(Guid id, string? remark);

    /// <summary>驳回待审核售后到可修改草稿并记录必填原因。</summary>
    Task<AfterSaleDto> RejectAsync(Guid id, string? remark);

    /// <summary>将已驳回并修正的售后草稿重新提交审核。</summary>
    Task<AfterSaleDto> ResubmitAsync(Guid id, string? remark);

    /// <summary>撤销尚未完成且未生成取货任务的审核结论，返回待审核。</summary>
    Task<AfterSaleDto> ReverseAsync(Guid id, string? remark);

    /// <summary>完成售后单；退货退款必须已完成取货并审核对应销售退货入库。</summary>
    Task<AfterSaleDto> CompleteAsync(Guid id);
}
