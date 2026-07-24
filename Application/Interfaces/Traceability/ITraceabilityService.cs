using Application.DTOs.Traceability;
using Application.QueryParameters;
using Application.QueryParameters.Traceability;
using Shared.Constants;

namespace Application.Interfaces;

/// <summary>溯源应用服务，维护检测报告并生成、查询销售订单商品的二维码溯源信息和报送结果。</summary>
public interface ITraceabilityService
{
    /// <summary>分页查询检测报告。</summary>
    Task<PagedResult<InspectionReportDto>> GetInspectionReportsAsync(InspectionReportQueryParameters parameters);
    /// <summary>读取包含商品和附件的检测报告详情。</summary>
    Task<InspectionReportDto> GetInspectionReportByIdAsync(Guid id);
    /// <summary>根据检测报告编号读取包含商品和附件的检测报告详情。</summary>
    Task<InspectionReportDto> GetInspectionReportByNoAsync(string inspectionNo);
    /// <summary>创建检测报告并固化已审核采购入库的商品、仓库和供应商快照。</summary>
    Task<InspectionReportDto> CreateInspectionReportAsync(SaveInspectionReportDto dto);
    /// <summary>编辑未被任何溯源记录引用的检测报告；一旦被引用，报告全文冻结以保护二维码历史。</summary>
    Task<InspectionReportDto> UpdateInspectionReportAsync(Guid id, SaveInspectionReportDto dto);
    /// <summary>删除检测报告；存在溯源引用时拒绝删除以保护二维码历史。</summary>
    Task DeleteInspectionReportAsync(Guid id);
    /// <summary>分页查询可用于创建报告的已审核采购入库单。</summary>
    Task<PagedResult<InspectionStockInOrderDto>> GetEligibleStockInOrdersAsync(PagedQueryParameters parameters);
    /// <summary>读取指定已审核采购入库单的商品明细，供客户端构造报告请求。</summary>
    Task<IReadOnlyList<InspectionStockInDetailDto>> GetEligibleStockInDetailsAsync(Guid stockInOrderId);
    /// <summary>为指定销售订单的已审核出库商品生成缺失溯源记录。</summary>
    Task<IReadOnlyList<TraceRecordDto>> GenerateSaleOrderTracesAsync(Guid saleOrderId);
    /// <summary>分页查询商品溯源记录。</summary>
    Task<PagedResult<TraceRecordDto>> GetTraceRecordsAsync(TraceRecordQueryParameters parameters);
    /// <summary>读取二维码详情。</summary>
    Task<TraceQrCodeDto> GetTraceQrCodeAsync(string traceNo);
    /// <summary>分页查询外部平台报送结果日志。</summary>
    Task<PagedResult<ExternalPushLogDto>> GetExternalPushLogsAsync(ExternalPushLogQueryParameters parameters);
}
