using System.Security.Cryptography;
using System.Text;
using Application.DTOs.Orders;
using Application.DTOs.Storage;
using Application.DTOs.Traceability;
using Application.interfaces;
using Domain.Entities.Orders;
using Domain.Entities.Storage;
using Domain.Entities.Traceability;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace SkyRoc.Tests.Testing.PostgreSql;

/// <summary>
///     基于受管采购入库批次形成检测报告，并通过真实销售出库生成可公开查询的溯源记录。
/// </summary>
internal sealed class DemoDataTraceabilityBuilder(
    ApplicationDbContext context,
    ITraceabilityService traceabilityService,
    ISaleOrderService saleOrderService,
    IStockOutService stockOutService,
    Guid auditUserId,
    string auditUsername)
{
    private const int InspectionReportCount = 50;
    private const int PurchaseStockInCount = 40;
    private const int TraceRecordCount = 20;

    /// <summary>
    ///     幂等补齐检测报告、报告商品、附件以及具备真实采购批次来源的销售溯源链路。
    /// </summary>
    /// <param name="cancellationToken">取消生成的令牌。</param>
    /// <returns>本层新增和复用的业务记录数量。</returns>
    public async Task<DemoDataTraceabilityGenerationResult> GenerateAsync(
        CancellationToken cancellationToken = default)
    {
        var purchaseStockIns = await LoadManagedPurchaseStockInsAsync(cancellationToken);
        var existingReports = await LoadManagedInspectionReportsAsync(cancellationToken);
        var createdReports = 0;
        var reusedReports = 0;
        var createdReportGoods = 0;
        var reusedReportGoods = 0;
        var createdAttachments = 0;
        var reusedAttachments = 0;

        for (var sequence = 1; sequence <= InspectionReportCount; sequence++)
        {
            var stockIn = purchaseStockIns[ResolveInspectionStockInIndex(sequence)];
            var expectedRemark = CreateInspectionReportRemark(sequence);
            if (!existingReports.TryGetValue(expectedRemark, out var report))
            {
                var created = await traceabilityService.CreateInspectionReportAsync(
                    CreateInspectionReportDto(stockIn, sequence));
                report = await LoadInspectionReportAsync(created.Id, cancellationToken);
                createdReports++;
                createdReportGoods += report.Goods.Count;
                createdAttachments += report.Attachments.Count;
                existingReports.Add(expectedRemark, report);
            }
            else
            {
                reusedReports++;
                reusedReportGoods += report.Goods.Count;
                reusedAttachments += report.Attachments.Count;
            }

            ValidateInspectionReport(report, stockIn, sequence);
        }

        var traceOrderKeys = Enumerable.Range(1, TraceRecordCount)
            .Select(sequence => DemoDataStableKeyCatalog.Create("TRACE-SALE-ORDER", sequence))
            .ToArray();
        var traceStockOutRemarks = Enumerable.Range(1, TraceRecordCount)
            .Select(CreateTraceStockOutRemark)
            .ToArray();
        await EnsureNoDriftedTraceKeysAsync(traceOrderKeys, traceStockOutRemarks, cancellationToken);

        var existingTraceOrders = await context.SaleOrders
            .Include(order => order.Details)
            .Include(order => order.AuditLogs)
            .Where(order => order.InnerRemark != null && traceOrderKeys.Contains(order.InnerRemark))
            .ToDictionaryAsync(order => order.InnerRemark!, StringComparer.Ordinal, cancellationToken);
        var existingTraceStockOuts = await context.StockOutOrders
            .Include(order => order.Details)
            .Where(order => order.Remark != null && traceStockOutRemarks.Contains(order.Remark))
            .ToDictionaryAsync(order => order.Remark!, StringComparer.Ordinal, cancellationToken);
        var customerCodes = Enumerable.Range(1, TraceRecordCount)
            .Select(sequence => DemoDataStableKeyCatalog.Create("CUSTOMER", sequence))
            .ToArray();
        var quotationCodes = Enumerable.Range(1, TraceRecordCount)
            .Select(sequence => DemoDataStableKeyCatalog.Create("QUOTATION", sequence))
            .ToArray();
        var departmentCodes = Enumerable.Range(1, TraceRecordCount)
            .Select(sequence => DemoDataStableKeyCatalog.Create("DEPARTMENT", sequence))
            .ToArray();
        var customers = await context.Customers
            .Where(customer => customerCodes.Contains(customer.Code))
            .ToDictionaryAsync(customer => customer.Code, StringComparer.Ordinal, cancellationToken);
        var quotations = await context.Quotations
            .Where(quotation => quotationCodes.Contains(quotation.Code))
            .ToDictionaryAsync(quotation => quotation.Code, StringComparer.Ordinal, cancellationToken);
        var departments = await context.Departments
            .Where(department => departmentCodes.Contains(department.Code))
            .ToDictionaryAsync(department => department.Code, StringComparer.Ordinal, cancellationToken);

        var createdTraceOrders = 0;
        var reusedTraceOrders = 0;
        var createdTraceOrderDetails = 0;
        var reusedTraceOrderDetails = 0;
        var createdTraceOrderAuditLogs = 0;
        var reusedTraceOrderAuditLogs = 0;
        var createdTraceStockOuts = 0;
        var reusedTraceStockOuts = 0;
        var createdTraceStockOutDetails = 0;
        var reusedTraceStockOutDetails = 0;
        var createdTraceStockOutLedgers = 0;
        var reusedTraceStockOutLedgers = 0;
        var createdTraceRecords = 0;
        var reusedTraceRecords = 0;

        for (var sequence = 1; sequence <= TraceRecordCount; sequence++)
        {
            var stockIn = purchaseStockIns[sequence - 1];
            var sourceDetail = stockIn.Details
                .OrderBy(detail => detail.GoodsCodeSnapshot, StringComparer.Ordinal)
                .First();
            if (!sourceDetail.StockBatchId.HasValue || sourceDetail.StockBatch is null)
            {
                throw new InvalidOperationException(
                    $"受管采购入库 {stockIn.Remark} 的溯源来源明细未关联库存批次。");
            }

            var customer = GetRequired(
                customers,
                DemoDataStableKeyCatalog.Create("CUSTOMER", sequence),
                "客户");
            var quotation = GetRequired(
                quotations,
                DemoDataStableKeyCatalog.Create("QUOTATION", sequence),
                "报价单");
            var department = GetRequired(
                departments,
                DemoDataStableKeyCatalog.Create("DEPARTMENT", sequence),
                "部门");
            var orderKey = DemoDataStableKeyCatalog.Create("TRACE-SALE-ORDER", sequence);
            var quantity = CreateTraceQuantity(sourceDetail);
            var fixedPrice = CreateTraceFixedPrice(sourceDetail, sequence);

            SaleOrder order;
            if (!existingTraceOrders.TryGetValue(orderKey, out var existingOrder))
            {
                var created = await saleOrderService.CreateAsync(CreateTraceSaleOrderDto(
                    stockIn,
                    sourceDetail,
                    customer.Id,
                    quotation.Id,
                    sequence,
                    quantity,
                    fixedPrice));
                await saleOrderService.ApproveAsync(created.Id, CreateTraceSaleOrderAuditRemark(sequence));
                order = await LoadTraceSaleOrderAsync(created.Id, cancellationToken);
                createdTraceOrders++;
                createdTraceOrderDetails += order.Details.Count;
                createdTraceOrderAuditLogs += order.AuditLogs.Count;
                existingTraceOrders.Add(orderKey, order);
            }
            else
            {
                order = existingOrder;
                if (order.OrderStatus == SaleOrderStatus.PendingAudit)
                {
                    await saleOrderService.ApproveAsync(order.Id, CreateTraceSaleOrderAuditRemark(sequence));
                    order = await LoadTraceSaleOrderAsync(order.Id, cancellationToken);
                }
                else if (order.OrderStatus == SaleOrderStatus.Rejected)
                {
                    throw new InvalidOperationException($"受管溯源销售订单 {orderKey} 已驳回，不能安全复用。");
                }

                reusedTraceOrders++;
                reusedTraceOrderDetails += order.Details.Count;
                reusedTraceOrderAuditLogs += order.AuditLogs.Count;
            }

            ValidateTraceSaleOrder(
                order,
                stockIn,
                sourceDetail,
                customer.Id,
                quotation.Id,
                sequence,
                quantity,
                fixedPrice);

            var stockOutRemark = CreateTraceStockOutRemark(sequence);
            StockOutOrder stockOut;
            if (!existingTraceStockOuts.TryGetValue(stockOutRemark, out var existingStockOut))
            {
                EnsureAvailableForTraceOutbound(sourceDetail, quantity);
                var created = await stockOutService.CreateSaleAsync(CreateTraceStockOutDto(
                    order,
                    sourceDetail,
                    department.Id,
                    sequence,
                    quantity,
                    fixedPrice));
                await stockOutService.AuditAsync(
                    StockOutOrderType.Sale,
                    created.Id,
                    CreateTraceStockOutAuditRemark(sequence));
                stockOut = await LoadTraceStockOutAsync(created.Id, cancellationToken);
                createdTraceStockOuts++;
                createdTraceStockOutDetails += stockOut.Details.Count;
                createdTraceStockOutLedgers += await context.StockLedgers
                    .CountAsync(ledger => ledger.SourceOrderId == stockOut.Id, cancellationToken);
                existingTraceStockOuts.Add(stockOutRemark, stockOut);
            }
            else
            {
                stockOut = existingStockOut;
                if (stockOut.BusinessStatus is StockDocumentStatus.Draft or StockDocumentStatus.PendingAudit)
                {
                    EnsureAvailableForTraceOutbound(sourceDetail, quantity);
                    await stockOutService.AuditAsync(
                        StockOutOrderType.Sale,
                        stockOut.Id,
                        CreateTraceStockOutAuditRemark(sequence));
                    stockOut = await LoadTraceStockOutAsync(stockOut.Id, cancellationToken);
                }
                else if (stockOut.BusinessStatus != StockDocumentStatus.Audited)
                {
                    throw new InvalidOperationException(
                        $"受管溯源销售出库 {stockOutRemark} 当前状态为 {stockOut.BusinessStatus}，不能安全复用。");
                }

                reusedTraceStockOuts++;
                reusedTraceStockOutDetails += stockOut.Details.Count;
                reusedTraceStockOutLedgers += await context.StockLedgers
                    .CountAsync(ledger => ledger.SourceOrderId == stockOut.Id, cancellationToken);
            }

            ValidateTraceStockOut(stockOut, order, sourceDetail, department.Id, sequence, quantity, fixedPrice);

            var existingTrace = await context.TraceRecords
                .AsNoTracking()
                .SingleOrDefaultAsync(trace => trace.SaleOrderDetailId == order.Details.Single().Id, cancellationToken);
            var generated = await traceabilityService.GenerateSaleOrderTracesAsync(order.Id);
            var traceDto = generated.Single();
            var trace = await context.TraceRecords
                .SingleAsync(item => item.Id == traceDto.Id, cancellationToken);
            var expectedTraceRemark = CreateTraceRecordRemark(sequence);
            if (existingTrace is null)
            {
                createdTraceRecords++;
            }
            else
            {
                reusedTraceRecords++;
            }

            if (trace.Remark is null)
            {
                trace.Remark = expectedTraceRemark;
                trace.UpdateBy = auditUserId;
                trace.UpdateName = auditUsername;
                await context.SaveChangesAsync(cancellationToken);
            }
            else if (!string.Equals(trace.Remark, expectedTraceRemark, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"受管溯源记录 {orderKey} 的稳定备注已漂移，拒绝修改既有记录。");
            }

            ValidateTraceRecord(trace, order, sourceDetail, existingReports[CreateInspectionReportRemark(sequence)], sequence);
        }

        return new DemoDataTraceabilityGenerationResult(
            createdReports,
            reusedReports,
            createdReportGoods,
            reusedReportGoods,
            createdAttachments,
            reusedAttachments,
            createdTraceOrders,
            reusedTraceOrders,
            createdTraceOrderDetails,
            reusedTraceOrderDetails,
            createdTraceOrderAuditLogs,
            reusedTraceOrderAuditLogs,
            createdTraceStockOuts,
            reusedTraceStockOuts,
            createdTraceStockOutDetails,
            reusedTraceStockOutDetails,
            createdTraceStockOutLedgers,
            reusedTraceStockOutLedgers,
            createdTraceRecords,
            reusedTraceRecords);
    }

    private async Task<IReadOnlyList<StockInOrder>> LoadManagedPurchaseStockInsAsync(
        CancellationToken cancellationToken)
    {
        var expectedRemarks = Enumerable.Range(1, PurchaseStockInCount)
            .Select(CreatePurchaseStockInRemark)
            .ToArray();
        var stockIns = await context.StockInOrders
            .AsNoTracking()
            .Include(order => order.Details)
            .ThenInclude(detail => detail.Goods)
            .Include(order => order.Details)
            .ThenInclude(detail => detail.GoodsUnit)
            .Include(order => order.Details)
            .ThenInclude(detail => detail.StockBatch)
            .Where(order => order.Remark != null && expectedRemarks.Contains(order.Remark))
            .OrderBy(order => order.Remark)
            .ToListAsync(cancellationToken);
        if (stockIns.Count != PurchaseStockInCount)
        {
            throw new InvalidOperationException(
                $"溯源生成需要 {PurchaseStockInCount} 张完整受管采购入库，当前为 {stockIns.Count} 张。");
        }

        for (var index = 0; index < stockIns.Count; index++)
        {
            var stockIn = stockIns[index];
            if (!string.Equals(stockIn.Remark, expectedRemarks[index], StringComparison.Ordinal)
                || stockIn.OrderType != StockInOrderType.Purchase
                || stockIn.BusinessStatus != StockDocumentStatus.Audited
                || stockIn.Details.Count != 2)
            {
                throw new InvalidOperationException(
                    $"受管采购入库 {expectedRemarks[index]} 的来源、状态或明细数量不符合溯源生成要求。");
            }
        }

        return stockIns;
    }

    private async Task<Dictionary<string, InspectionReport>> LoadManagedInspectionReportsAsync(
        CancellationToken cancellationToken)
    {
        var expectedRemarks = Enumerable.Range(1, InspectionReportCount)
            .Select(CreateInspectionReportRemark)
            .ToHashSet(StringComparer.Ordinal);
        var prefix = $"{DemoDataStableKeyCatalog.ManagedPrefix}-INSPECTION-REPORT-";
        var reports = await context.InspectionReports
            .AsNoTracking()
            .Include(report => report.Goods)
            .Include(report => report.Attachments)
            .Where(report => report.Remark != null && report.Remark.StartsWith(prefix))
            .ToListAsync(cancellationToken);
        var drifted = reports.Select(report => report.Remark!).FirstOrDefault(remark => !expectedRemarks.Contains(remark));
        if (drifted is not null)
        {
            throw new InvalidOperationException($"检测到未知或漂移的受管检测报告备注 {drifted}，拒绝模糊复用。");
        }

        return reports.ToDictionary(report => report.Remark!, StringComparer.Ordinal);
    }

    private async Task EnsureNoDriftedTraceKeysAsync(
        IReadOnlyCollection<string> expectedOrderKeys,
        IReadOnlyCollection<string> expectedStockOutRemarks,
        CancellationToken cancellationToken)
    {
        var orderPrefix = $"{DemoDataStableKeyCatalog.ManagedPrefix}-TRACE-SALE-ORDER-";
        var orderKeys = await context.SaleOrders
            .AsNoTracking()
            .Where(order => order.InnerRemark != null && order.InnerRemark.StartsWith(orderPrefix))
            .Select(order => order.InnerRemark!)
            .ToListAsync(cancellationToken);
        var driftedOrderKey = orderKeys.FirstOrDefault(key => !expectedOrderKeys.Contains(key, StringComparer.Ordinal));
        if (driftedOrderKey is not null)
        {
            throw new InvalidOperationException($"检测到未知受管溯源销售订单键 {driftedOrderKey}，拒绝模糊复用。");
        }

        var stockOutPrefix = $"{DemoDataStableKeyCatalog.ManagedPrefix}-TRACE-STOCK-OUT-";
        var stockOutRemarks = await context.StockOutOrders
            .AsNoTracking()
            .Where(order => order.Remark != null && order.Remark.StartsWith(stockOutPrefix))
            .Select(order => order.Remark!)
            .ToListAsync(cancellationToken);
        var driftedStockOut = stockOutRemarks.FirstOrDefault(
            remark => !expectedStockOutRemarks.Contains(remark, StringComparer.Ordinal));
        if (driftedStockOut is not null)
        {
            throw new InvalidOperationException($"检测到未知受管溯源销售出库备注 {driftedStockOut}，拒绝模糊复用。");
        }
    }

    private async Task<InspectionReport> LoadInspectionReportAsync(Guid id, CancellationToken cancellationToken)
    {
        return await context.InspectionReports
            .AsNoTracking()
            .Include(report => report.Goods)
            .Include(report => report.Attachments)
            .SingleAsync(report => report.Id == id, cancellationToken);
    }

    private async Task<SaleOrder> LoadTraceSaleOrderAsync(Guid id, CancellationToken cancellationToken)
    {
        return await context.SaleOrders
            .AsNoTracking()
            .Include(order => order.Details)
            .Include(order => order.AuditLogs)
            .SingleAsync(order => order.Id == id, cancellationToken);
    }

    private async Task<StockOutOrder> LoadTraceStockOutAsync(Guid id, CancellationToken cancellationToken)
    {
        return await context.StockOutOrders
            .AsNoTracking()
            .Include(order => order.Details)
            .SingleAsync(order => order.Id == id, cancellationToken);
    }

    private static SaveInspectionReportDto CreateInspectionReportDto(StockInOrder stockIn, int sequence)
    {
        var conclusion = CreateInspectionConclusion(sequence);
        return new SaveInspectionReportDto
        {
            StockInOrderId = stockIn.Id,
            InspectionOrg = $"华东农产品质量检测中心第 {(sequence - 1) % 5 + 1} 实验室",
            SampleTime = CreateInspectionSampleTime(sequence),
            InspectTime = CreateInspectionTime(sequence),
            Conclusion = conclusion,
            Remark = CreateInspectionReportRemark(sequence),
            Goods = stockIn.Details
                .OrderBy(detail => detail.GoodsCodeSnapshot, StringComparer.Ordinal)
                .Select((detail, index) => new SaveInspectionReportGoodsDto
                {
                    StockInDetailId = detail.Id,
                    SampleQuantity = NumericPrecision.RoundQuantity(detail.Quantity * (0.2m + index * 0.05m)),
                    Conclusion = conclusion == InspectionConclusion.Unqualified && index == 1
                        ? InspectionConclusion.Qualified
                        : conclusion,
                    Remark = $"华东联调抽检说明：第 {sequence:D2} 份报告第 {index + 1} 项商品按入库批次留样并完成感官与农残检测。"
                })
                .ToList(),
            Attachments =
            [
                new SaveInspectionAttachmentDto
                {
                    AttachmentType = InspectionAttachmentType.Report,
                    FileName = $"华东鲜品检测报告-{sequence:D3}.pdf",
                    FileUrl = CreateProtectedFileUrl("INSPECTION-PDF", sequence),
                    FileSize = 256_000L + sequence * 1_024L,
                    Sort = 0
                },
                new SaveInspectionAttachmentDto
                {
                    AttachmentType = InspectionAttachmentType.Image,
                    FileName = $"华东鲜品抽样现场-{sequence:D3}.jpg",
                    FileUrl = CreateProtectedFileUrl("INSPECTION-IMAGE", sequence),
                    FileSize = 512_000L + sequence * 2_048L,
                    Sort = 1
                }
            ]
        };
    }

    private static CreateSaleOrderDto CreateTraceSaleOrderDto(
        StockInOrder stockIn,
        StockInDetail sourceDetail,
        Guid customerId,
        Guid quotationId,
        int sequence,
        decimal quantity,
        decimal fixedPrice)
    {
        return new CreateSaleOrderDto
        {
            CustomerId = customerId,
            QuotationId = quotationId,
            WareId = stockIn.WareId,
            OrderDate = CreateTraceOrderTime(sequence),
            ReceiveDate = CreateTraceOrderTime(sequence).AddHours(8),
            ContactName = $"溯源联调收货人{sequence:D2}",
            ContactPhone = $"1397000{sequence:D4}",
            DeliveryAddress = $"上海市浦东新区溯源示范街{sequence}号质量验收区",
            Remark = CreateTraceSaleOrderRemark(sequence),
            InnerRemark = DemoDataStableKeyCatalog.Create("TRACE-SALE-ORDER", sequence),
            Details =
            [
                new CreateSaleOrderDetailDto
                {
                    GoodsId = sourceDetail.GoodsId,
                    GoodsUnitId = sourceDetail.GoodsUnitId,
                    Quantity = quantity,
                    FixedPrice = fixedPrice,
                    FixedGoodsUnitId = sourceDetail.GoodsUnitId,
                    Remark = $"华东联调溯源订单商品：使用采购入库批次 {sourceDetail.BatchNo} 验证来源链。",
                    InnerRemark = $"华东联调溯源订单内部说明：第 {sequence:D2} 项商品仅用于真实批次溯源展示。"
                }
            ]
        };
    }

    private static CreateSaleStockOutDto CreateTraceStockOutDto(
        SaleOrder order,
        StockInDetail sourceDetail,
        Guid departmentId,
        int sequence,
        decimal quantity,
        decimal fixedPrice)
    {
        return new CreateSaleStockOutDto
        {
            WareId = order.WareId!.Value,
            SaleOrderId = order.Id,
            CustomerId = order.CustomerId,
            DepartmentId = departmentId,
            OutTime = CreateTraceOrderTime(sequence).AddHours(4),
            Remark = CreateTraceStockOutRemark(sequence),
            Details =
            [
                new CreateStockOutDetailDto
                {
                    SaleOrderDetailId = order.Details.Single().Id,
                    StockBatchId = sourceDetail.StockBatchId!.Value,
                    GoodsUnitId = sourceDetail.GoodsUnitId,
                    Quantity = quantity,
                    UnitPrice = fixedPrice,
                    Remark = $"华东联调溯源销售出库明细：精确扣减采购批次 {sourceDetail.BatchNo}。"
                }
            ]
        };
    }

    private void ValidateInspectionReport(InspectionReport report, StockInOrder stockIn, int sequence)
    {
        var expected = CreateInspectionReportDto(stockIn, sequence);
        if (report.StockInOrderId != stockIn.Id
            || report.InNoSnapshot != stockIn.InNo
            || report.WareId != stockIn.WareId
            || report.WareNameSnapshot != stockIn.WareNameSnapshot
            || report.SupplierId != stockIn.SupplierId
            || report.SupplierNameSnapshot != stockIn.SupplierNameSnapshot
            || report.InspectionOrg != expected.InspectionOrg
            || report.SampleTime != expected.SampleTime
            || report.InspectTime != expected.InspectTime
            || report.Conclusion != expected.Conclusion
            || report.Remark != expected.Remark
            || report.CreateBy != auditUserId
            || report.CreateName != auditUsername)
        {
            throw new InvalidOperationException(
                $"受管检测报告 {expected.Remark} 的来源、快照、结论或审计指纹已漂移。");
        }

        var actualGoods = report.Goods.OrderBy(goods => goods.StockInDetailId).ToArray();
        var expectedGoods = expected.Goods.OrderBy(goods => goods.StockInDetailId).ToArray();
        if (actualGoods.Length != expectedGoods.Length)
        {
            throw new InvalidOperationException($"受管检测报告 {expected.Remark} 的商品明细数量已漂移。");
        }

        for (var index = 0; index < actualGoods.Length; index++)
        {
            var actual = actualGoods[index];
            var expectedItem = expectedGoods[index];
            var source = stockIn.Details.Single(detail => detail.Id == expectedItem.StockInDetailId);
            if (actual.StockInDetailId != source.Id
                || actual.GoodsId != source.GoodsId
                || actual.GoodsNameSnapshot != source.GoodsNameSnapshot
                || actual.GoodsCodeSnapshot != source.GoodsCodeSnapshot
                || actual.GoodsUnitId != source.GoodsUnitId
                || actual.GoodsUnitNameSnapshot != source.GoodsUnitNameSnapshot
                || actual.SampleQuantity != expectedItem.SampleQuantity
                || actual.BatchNoSnapshot != source.BatchNo
                || actual.Conclusion != expectedItem.Conclusion
                || actual.Remark != expectedItem.Remark
                || actual.CreateBy != auditUserId
                || actual.CreateName != auditUsername)
            {
                throw new InvalidOperationException($"受管检测报告 {expected.Remark} 的商品快照或审计指纹已漂移。");
            }
        }

        var actualAttachments = report.Attachments.OrderBy(attachment => attachment.Sort).ToArray();
        var expectedAttachments = expected.Attachments.OrderBy(attachment => attachment.Sort).ToArray();
        if (actualAttachments.Length != expectedAttachments.Length)
        {
            throw new InvalidOperationException($"受管检测报告 {expected.Remark} 的附件数量已漂移。");
        }

        for (var index = 0; index < actualAttachments.Length; index++)
        {
            var actual = actualAttachments[index];
            var expectedItem = expectedAttachments[index];
            if (actual.AttachmentType != expectedItem.AttachmentType
                || actual.FileName != expectedItem.FileName
                || actual.FileUrl != expectedItem.FileUrl
                || actual.FileSize != expectedItem.FileSize
                || actual.Sort != expectedItem.Sort
                || actual.CreateBy != auditUserId
                || actual.CreateName != auditUsername)
            {
                throw new InvalidOperationException($"受管检测报告 {expected.Remark} 的附件或审计指纹已漂移。");
            }
        }
    }

    private void ValidateTraceSaleOrder(
        SaleOrder order,
        StockInOrder stockIn,
        StockInDetail sourceDetail,
        Guid customerId,
        Guid quotationId,
        int sequence,
        decimal quantity,
        decimal fixedPrice)
    {
        var detail = order.Details.SingleOrDefault()
                     ?? throw new InvalidOperationException($"受管溯源销售订单 {order.InnerRemark} 必须仅有一条商品明细。");
        if (order.InnerRemark != DemoDataStableKeyCatalog.Create("TRACE-SALE-ORDER", sequence)
            || order.CustomerId != customerId
            || order.QuotationId != quotationId
            || order.WareId != stockIn.WareId
            || order.OrderDate != CreateTraceOrderTime(sequence)
            || order.ReceiveDate != CreateTraceOrderTime(sequence).AddHours(8)
            || order.ContactNameSnapshot != $"溯源联调收货人{sequence:D2}"
            || order.ContactPhoneSnapshot != $"1397000{sequence:D4}"
            || order.DeliveryAddressSnapshot != $"上海市浦东新区溯源示范街{sequence}号质量验收区"
            || order.Remark != CreateTraceSaleOrderRemark(sequence)
            || order.OrderStatus is SaleOrderStatus.PendingAudit or SaleOrderStatus.Rejected
            || order.CreateBy != auditUserId
            || order.CreateName != auditUsername
            || detail.GoodsId != sourceDetail.GoodsId
            || detail.GoodsUnitId != sourceDetail.GoodsUnitId
            || detail.Quantity != quantity
            || detail.FixedPrice != fixedPrice
            || detail.CreateBy != auditUserId
            || detail.CreateName != auditUsername)
        {
            throw new InvalidOperationException(
                $"受管溯源销售订单 {order.InnerRemark} 的来源、商品、状态或审计指纹已漂移。");
        }
    }

    private static void ValidateTraceStockOut(
        StockOutOrder stockOut,
        SaleOrder order,
        StockInDetail sourceDetail,
        Guid departmentId,
        int sequence,
        decimal quantity,
        decimal fixedPrice)
    {
        var detail = stockOut.Details.SingleOrDefault()
                     ?? throw new InvalidOperationException($"受管溯源销售出库 {stockOut.Remark} 必须仅有一条商品明细。");
        if (stockOut.OrderType != StockOutOrderType.Sale
            || stockOut.BusinessStatus != StockDocumentStatus.Audited
            || stockOut.WareId != order.WareId
            || stockOut.SaleOrderId != order.Id
            || stockOut.CustomerId != order.CustomerId
            || stockOut.DepartmentId != departmentId
            || stockOut.OutTime != CreateTraceOrderTime(sequence).AddHours(4)
            || stockOut.Remark != CreateTraceStockOutRemark(sequence)
            || detail.SaleOrderDetailId != order.Details.Single().Id
            || detail.StockBatchId != sourceDetail.StockBatchId
            || detail.GoodsUnitId != sourceDetail.GoodsUnitId
            || detail.Quantity != quantity
            || detail.UnitPrice != fixedPrice)
        {
            throw new InvalidOperationException($"受管溯源销售出库 {stockOut.Remark} 的来源、批次或金额已漂移。");
        }
    }

    private void ValidateTraceRecord(
        TraceRecord trace,
        SaleOrder order,
        StockInDetail sourceDetail,
        InspectionReport report,
        int sequence)
    {
        var saleDetail = order.Details.Single();
        if (trace.SaleOrderId != order.Id
            || trace.SaleOrderDetailId != saleDetail.Id
            || trace.CustomerId != order.CustomerId
            || trace.GoodsId != saleDetail.GoodsId
            || trace.StockInDetailId != sourceDetail.Id
            || trace.SupplierId != report.SupplierId
            || trace.WareId != report.WareId
            || trace.BatchNoSnapshot != sourceDetail.BatchNo
            || trace.InspectionReportId != report.Id
            || trace.Remark != CreateTraceRecordRemark(sequence)
            || trace.CreateBy != auditUserId
            || trace.CreateName != auditUsername)
        {
            throw new InvalidOperationException(
                $"受管溯源记录 {CreateTraceRecordRemark(sequence)} 的销售、采购、报告或审计来源已漂移。");
        }
    }

    private static decimal CreateTraceQuantity(StockInDetail sourceDetail)
    {
        const decimal quantity = 0.5m;
        var rounded = NumericPrecision.RoundQuantity(quantity);
        if (NumericPrecision.RoundQuantity(rounded * sourceDetail.ConversionRate) <= 0m)
        {
            throw new InvalidOperationException($"采购入库明细 {sourceDetail.Id} 的单位换算无法形成有效溯源出库数量。");
        }

        return rounded;
    }

    private static decimal CreateTraceFixedPrice(StockInDetail sourceDetail, int sequence)
    {
        return NumericPrecision.RoundMoney(sourceDetail.UnitPrice + 2.75m + sequence * 0.01m);
    }

    private static void EnsureAvailableForTraceOutbound(StockInDetail sourceDetail, decimal quantity)
    {
        var requiredBaseQuantity = NumericPrecision.RoundQuantity(quantity * sourceDetail.ConversionRate);
        if (sourceDetail.StockBatch!.AvailableQuantity < requiredBaseQuantity)
        {
            throw new InvalidOperationException(
                $"采购批次 {sourceDetail.BatchNo} 可用库存不足，不能安全创建受管溯源销售出库。");
        }
    }

    private static InspectionConclusion CreateInspectionConclusion(int sequence)
    {
        return sequence switch
        {
            <= 30 => InspectionConclusion.Qualified,
            <= 35 => InspectionConclusion.Pending,
            <= 40 => InspectionConclusion.Unqualified,
            _ => InspectionConclusion.Qualified
        };
    }

    private static DateTime CreateInspectionSampleTime(int sequence)
    {
        return sequence <= PurchaseStockInCount
            ? new DateTime(2026, 8, (sequence - 1) % 28 + 1, 13, 0, 0, DateTimeKind.Utc)
            : new DateTime(2026, 9, sequence - PurchaseStockInCount, 13, 0, 0, DateTimeKind.Utc);
    }

    private static int ResolveInspectionStockInIndex(int sequence)
    {
        return sequence <= PurchaseStockInCount
            ? sequence - 1
            : sequence - 21;
    }

    private static DateTime CreateInspectionTime(int sequence)
    {
        return CreateInspectionSampleTime(sequence).AddHours(3);
    }

    private static DateTime CreateTraceOrderTime(int sequence)
    {
        return new DateTime(2026, 9, (sequence - 1) % 20 + 1, 8, 30, 0, DateTimeKind.Utc);
    }

    private static string CreatePurchaseStockInRemark(int sequence)
    {
        var stableKey = DemoDataStableKeyCatalog.Create("PURCHASE-STOCK-IN", sequence);
        return $"{stableKey} 华东联调采购入库{sequence:D2}：来源受管采购单，用于库存批次、流水和供应商待结链路。";
    }

    private static string CreateInspectionReportRemark(int sequence)
    {
        var stableKey = DemoDataStableKeyCatalog.Create("INSPECTION-REPORT", sequence);
        return $"{stableKey} 华东联调检测报告{sequence:D2}：记录受管采购入库商品抽检结论与附件快照。";
    }

    private static string CreateTraceSaleOrderRemark(int sequence)
    {
        return $"华东联调溯源销售订单{sequence:D2}：销售已检测采购批次商品并形成可核对的溯源来源。";
    }

    private static string CreateTraceSaleOrderAuditRemark(int sequence)
    {
        return $"华东联调溯源订单审核：确认第 {sequence:D2} 张订单可从已检测采购批次办理销售出库。";
    }

    private static string CreateTraceStockOutRemark(int sequence)
    {
        var stableKey = DemoDataStableKeyCatalog.Create("TRACE-STOCK-OUT", sequence);
        return $"{stableKey} 华东联调溯源销售出库{sequence:D2}：从已检测采购批次出库以形成真实溯源来源。";
    }

    private static string CreateTraceStockOutAuditRemark(int sequence)
    {
        return $"华东联调溯源销售出库审核：核准第 {sequence:D2} 张溯源订单扣减对应采购批次。";
    }

    private static string CreateTraceRecordRemark(int sequence)
    {
        var stableKey = DemoDataStableKeyCatalog.Create("TRACE-RECORD", sequence);
        return $"{stableKey} 华东联调溯源记录{sequence:D2}：串联销售商品、采购批次与检测报告。";
    }

    private static string CreateProtectedFileUrl(string area, int sequence)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(DemoDataStableKeyCatalog.Create(area, sequence)));
        var fileId = new Guid(bytes.AsSpan(0, 16));
        return $"/api/files/{fileId}/download";
    }

    private static TValue GetRequired<TKey, TValue>(
        IReadOnlyDictionary<TKey, TValue> entities,
        TKey key,
        string businessName)
        where TKey : notnull
        where TValue : class
    {
        return entities.TryGetValue(key, out var value)
            ? value
            : throw new InvalidOperationException($"未找到键为 {key} 的受管{businessName}。");
    }
}
