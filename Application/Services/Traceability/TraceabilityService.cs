using Application.DTOs.Traceability;
using Application.Exceptions;
using Application.interfaces;
using Application.QueryParameters;
using Application.QueryParameters.Traceability;
using Domain.Entities;
using Domain.Entities.Storage;
using Domain.Entities.Traceability;
using Domain.Interfaces;
using FluentValidation;
using Shared.Constants;
using ValidationException = Application.Exceptions.ValidationException;

namespace Application.Services;

/// <summary>溯源服务，维护检测报告快照，并从已审核销售出库的批次来源生成可公开查询的二维码溯源记录。</summary>
public class TraceabilityService(
    IInspectionReportRepository inspectionReportRepository,
    ITraceRecordRepository traceRecordRepository,
    IExternalPushLogRepository externalPushLogRepository,
    IStockInOrderRepository stockInOrderRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    IValidator<SaveInspectionReportDto> reportValidator) : ITraceabilityService
{
    /// <inheritdoc />
    public async Task<PagedResult<InspectionReportDto>> GetInspectionReportsAsync(InspectionReportQueryParameters parameters)
    {
        var result = await inspectionReportRepository.GetPagedAsync(
            parameters.QueryBuild(), parameters.Current, parameters.Size, x => x.InspectTime, true);
        return ToPaged(result.Data.Select(ToDto), result.Total, parameters);
    }

    /// <inheritdoc />
    public async Task<InspectionReportDto> GetInspectionReportByIdAsync(Guid id)
    {
        var report = await inspectionReportRepository.GetDetailByIdAsync(id)
                     ?? throw new NotFoundException("检测报告不存在");
        return ToDto(report);
    }

    /// <inheritdoc />
    public async Task<InspectionReportDto> CreateInspectionReportAsync(SaveInspectionReportDto dto)
    {
        await ValidateAsync(dto);
        var reportId = Guid.NewGuid();
        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var stockInOrder = await GetEligibleStockInOrderForUpdateAsync(dto.StockInOrderId);
            var report = new InspectionReport
            {
                Id = reportId,
                InspectionNo = await GenerateInspectionNoAsync(),
                StockInOrderId = stockInOrder.Id,
                InNoSnapshot = stockInOrder.InNo,
                WareId = stockInOrder.WareId,
                WareNameSnapshot = stockInOrder.WareNameSnapshot,
                SupplierId = stockInOrder.SupplierId,
                SupplierNameSnapshot = stockInOrder.SupplierNameSnapshot,
                InspectionOrg = dto.InspectionOrg.Trim(),
                SampleTime = dto.SampleTime,
                InspectTime = dto.InspectTime,
                Conclusion = dto.Conclusion,
                Remark = Normalize(dto.Remark),
                Goods = BuildGoods(dto.Goods, stockInOrder),
                Attachments = BuildAttachments(dto.Attachments)
            };
            ApplyCreateAudit(report);
            foreach (var goods in report.Goods) ApplyCreateAudit(goods);
            foreach (var attachment in report.Attachments) ApplyCreateAudit(attachment);
            await inspectionReportRepository.AddAsync(report);
        });
        return await GetInspectionReportByIdAsync(reportId);
    }

    /// <inheritdoc />
    public async Task<InspectionReportDto> UpdateInspectionReportAsync(Guid id, SaveInspectionReportDto dto)
    {
        await ValidateAsync(dto);
        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var report = await inspectionReportRepository.GetDetailByIdForUpdateAsync(id)
                         ?? throw new NotFoundException("检测报告不存在");
            if (await traceRecordRepository.ExistsByInspectionReportIdAsync(id))
            {
                throw new BusinessException("报告已被溯源引用，不可修改");
            }
            if (report.StockInOrderId != dto.StockInOrderId)
            {
                throw new BusinessException("检测报告来源采购入库单不可修改");
            }

            var stockInOrder = await GetEligibleStockInOrderForUpdateAsync(dto.StockInOrderId);
            report.InspectionOrg = dto.InspectionOrg.Trim();
            report.SampleTime = dto.SampleTime;
            report.InspectTime = dto.InspectTime;
            report.Conclusion = dto.Conclusion;
            report.Remark = Normalize(dto.Remark);
            report.Goods.Clear();
            report.Attachments.Clear();
            foreach (var goods in BuildGoods(dto.Goods, stockInOrder))
            {
                ApplyCreateAudit(goods);
                report.Goods.Add(goods);
            }
            foreach (var attachment in BuildAttachments(dto.Attachments))
            {
                ApplyCreateAudit(attachment);
                report.Attachments.Add(attachment);
            }
            ApplyUpdateAudit(report);
        });
        return await GetInspectionReportByIdAsync(id);
    }

    /// <inheritdoc />
    public async Task DeleteInspectionReportAsync(Guid id)
    {
        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var report = await inspectionReportRepository.GetDetailByIdForUpdateAsync(id)
                         ?? throw new NotFoundException("检测报告不存在");
            if (await traceRecordRepository.ExistsByInspectionReportIdAsync(id))
            {
                throw new BusinessException("报告已被溯源引用，不可删除");
            }
            await inspectionReportRepository.DeleteAsync(report);
        });
    }

    /// <inheritdoc />
    public async Task<PagedResult<InspectionStockInOrderDto>> GetEligibleStockInOrdersAsync(PagedQueryParameters parameters)
    {
        var result = await stockInOrderRepository.GetPagedAsync(
            x => x.OrderType == StockInOrderType.Purchase && x.BusinessStatus == StockDocumentStatus.Audited,
            parameters.Current, parameters.Size, x => x.AuditTime!, true);
        return ToPaged(result.Data.Select(x => new InspectionStockInOrderDto
        {
            Id = x.Id, InNo = x.InNo, WareId = x.WareId, WareName = x.WareNameSnapshot,
            SupplierId = x.SupplierId, SupplierName = x.SupplierNameSnapshot, AuditTime = x.AuditTime
        }), result.Total, parameters);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<InspectionStockInDetailDto>> GetEligibleStockInDetailsAsync(Guid stockInOrderId)
    {
        var order = await stockInOrderRepository.GetByIdAsync(stockInOrderId)
                    ?? throw new NotFoundException("采购入库单不存在");
        EnsureEligibleStockInOrder(order);
        return order.Details.OrderBy(x => x.Id).Select(x => new InspectionStockInDetailDto
        {
            Id = x.Id, GoodsId = x.GoodsId, GoodsName = x.GoodsNameSnapshot, GoodsCode = x.GoodsCodeSnapshot,
            GoodsTypeName = x.Goods.GoodsType.Name, GoodsUnitId = x.GoodsUnitId,
            GoodsUnitName = x.GoodsUnitNameSnapshot, Quantity = x.Quantity, BatchNo = x.BatchNo
        }).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TraceRecordDto>> GenerateSaleOrderTracesAsync(Guid saleOrderId)
    {
        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            if (!await traceRecordRepository.LockSaleOrderAsync(saleOrderId))
            {
                throw new NotFoundException("销售订单不存在");
            }
            var sources = await traceRecordRepository.GetGenerationSourcesAsync(saleOrderId);
            if (sources.Count == 0)
            {
                throw new BusinessException("订单没有已审核销售出库商品，无法生成溯源记录");
            }
            var reportIds = sources.Where(x => x.InspectionReportId.HasValue)
                .Select(x => x.InspectionReportId!.Value)
                .Distinct()
                .ToArray();
            var lockedReportIds = await inspectionReportRepository.LockByIdsAsync(reportIds);
            if (lockedReportIds.Count != reportIds.Length)
            {
                throw new BusinessException("关联检测报告已被删除，请重新生成溯源记录");
            }
            var lockedReportGoods = await inspectionReportRepository.GetLockedGoodsSourceIdsAsync(lockedReportIds);
            var lockedReportGoodsSet = lockedReportGoods.ToHashSet();
            if (sources.Any(x => x.InspectionReportId.HasValue
                                 && !lockedReportGoodsSet.Contains((x.InspectionReportId.Value, x.StockInDetailId))))
            {
                throw new BusinessException("关联检测报告商品已变更，请重新生成溯源记录");
            }

            var existing = await traceRecordRepository.GetBySaleOrderIdAsync(saleOrderId);
            var existingDetailIds = existing.Select(x => x.SaleOrderDetailId).ToHashSet();
            foreach (var group in sources.GroupBy(x => x.SaleOrderDetailId).OrderBy(x => x.Key))
            {
                if (group.Any(x => x.StockInSourceCount != 1))
                {
                    throw new BusinessException("销售出库批次缺少或存在多个已审核采购入库来源，无法安全生成溯源记录");
                }
                var sourceRows = group.Select(x => x.StockInDetailId).Distinct().ToArray();
                if (sourceRows.Length != 1)
                {
                    throw new BusinessException("订单商品关联多个采购入库批次，当前单商品溯源无法安全生成");
                }
                if (existingDetailIds.Contains(group.Key)) continue;
                var source = group.First();
                var trace = new TraceRecord
                {
                    Id = Guid.NewGuid(), TraceNo = await GenerateTraceNoAsync(), SaleOrderId = source.SaleOrderId,
                    SaleOrderNoSnapshot = source.SaleOrderNo, SaleOrderDetailId = source.SaleOrderDetailId,
                    CustomerId = source.CustomerId, CustomerNameSnapshot = source.CustomerName, GoodsId = source.GoodsId,
                    GoodsNameSnapshot = source.GoodsName, GoodsCodeSnapshot = source.GoodsCode,
                    GoodsTypeNameSnapshot = source.GoodsTypeName, StockInDetailId = source.StockInDetailId,
                    SupplierId = source.SupplierId, SupplierNameSnapshot = source.SupplierName, WareId = source.WareId,
                    WareNameSnapshot = source.WareName, BatchNoSnapshot = source.BatchNo,
                    InspectionReportId = source.InspectionReportId
                };
                ApplyCreateAudit(trace);
                await traceRecordRepository.AddAsync(trace);
            }
        });
        return (await traceRecordRepository.GetBySaleOrderIdAsync(saleOrderId)).Select(ToDto).ToList();
    }

    /// <inheritdoc />
    public async Task<PagedResult<TraceRecordDto>> GetTraceRecordsAsync(TraceRecordQueryParameters parameters)
    {
        var result = await traceRecordRepository.GetPagedAsync(
            parameters.QueryBuild(), parameters.Current, parameters.Size, x => x.CreateTime!, true);
        return ToPaged(result.Data.Select(ToDto), result.Total, parameters);
    }

    /// <inheritdoc />
    public async Task<TraceQrCodeDto> GetTraceQrCodeAsync(string traceNo)
    {
        if (string.IsNullOrWhiteSpace(traceNo)) throw new BusinessException("溯源编号不能为空");
        var trace = await traceRecordRepository.GetDetailByTraceNoAsync(traceNo)
                    ?? throw new NotFoundException("溯源记录不存在");
        return new TraceQrCodeDto
        {
            TraceRecord = new PublicTraceQrRecordDto
            {
                TraceNo = trace.TraceNo, GoodsName = trace.GoodsNameSnapshot, GoodsCode = trace.GoodsCodeSnapshot,
                GoodsTypeName = trace.GoodsTypeNameSnapshot, SupplierName = trace.SupplierNameSnapshot,
                WareName = trace.WareNameSnapshot, BatchNo = trace.BatchNoSnapshot
            },
            InspectionReport = trace.InspectionReport is null ? null : new PublicInspectionReportDto
            {
                InspectionNo = trace.InspectionReport.InspectionNo,
                InspectionOrg = trace.InspectionReport.InspectionOrg,
                InspectTime = trace.InspectionReport.InspectTime,
                Conclusion = trace.InspectionReport.Conclusion,
                Goods = trace.InspectionReport.Goods.OrderBy(x => x.StockInDetailId).Select(x => new PublicInspectionReportGoodsDto
                {
                    GoodsName = x.GoodsNameSnapshot, GoodsCode = x.GoodsCodeSnapshot,
                    GoodsTypeName = x.GoodsTypeNameSnapshot, SampleQuantity = x.SampleQuantity,
                    GoodsUnitName = x.GoodsUnitNameSnapshot, BatchNo = x.BatchNoSnapshot, Conclusion = x.Conclusion
                }).ToList()
            }
        };
    }

    /// <inheritdoc />
    public async Task<PagedResult<ExternalPushLogDto>> GetExternalPushLogsAsync(ExternalPushLogQueryParameters parameters)
    {
        var result = await externalPushLogRepository.GetPagedAsync(
            parameters.QueryBuild(), parameters.Current, parameters.Size, x => x.PushTime, true);
        return ToPaged(result.Data.Select(x => new ExternalPushLogDto
        {
            Id = x.Id, BusinessType = x.BusinessType, BusinessId = x.BusinessId, BusinessNo = x.BusinessNoSnapshot,
            PlatformCode = x.PlatformCode, PushStatus = x.PushStatus, PushTime = x.PushTime, ResponseTime = x.ResponseTime,
            ErrorMessage = x.ErrorMessage, RetryCount = x.RetryCount, CreateTime = x.CreateTime
        }), result.Total, parameters);
    }

    private async Task<StockInOrder> GetEligibleStockInOrderForUpdateAsync(Guid id)
    {
        var order = await stockInOrderRepository.GetByIdForUpdateAsync(id)
                    ?? throw new NotFoundException("采购入库单不存在");
        EnsureEligibleStockInOrder(order);
        return order;
    }

    private static void EnsureEligibleStockInOrder(StockInOrder order)
    {
        if (order.OrderType != StockInOrderType.Purchase || order.BusinessStatus != StockDocumentStatus.Audited)
        {
            throw new BusinessException("只能选择已审核的采购入库单创建检测报告");
        }
    }

    private static List<InspectionReportGoods> BuildGoods(IEnumerable<SaveInspectionReportGoodsDto> inputs, StockInOrder order)
    {
        var inputList = inputs.ToList();
        if (inputList.Select(x => x.StockInDetailId).Distinct().Count() != inputList.Count)
        {
            throw new BusinessException("送检商品不能重复选择同一入库明细");
        }
        var details = order.Details.ToDictionary(x => x.Id);
        var goods = new List<InspectionReportGoods>();
        foreach (var input in inputList)
        {
            if (!details.TryGetValue(input.StockInDetailId, out var detail))
            {
                throw new BusinessException("送检商品必须来自所选采购入库单");
            }
            var quantity = NumericPrecision.RoundQuantity(input.SampleQuantity);
            if (quantity <= 0m || quantity > detail.Quantity)
            {
                throw new BusinessException("送检数量必须大于零且不能超过来源入库数量");
            }
            goods.Add(new InspectionReportGoods
            {
                Id = Guid.NewGuid(), StockInDetailId = detail.Id, GoodsId = detail.GoodsId,
                GoodsNameSnapshot = detail.GoodsNameSnapshot, GoodsCodeSnapshot = detail.GoodsCodeSnapshot,
                GoodsTypeNameSnapshot = detail.Goods.GoodsType.Name, GoodsUnitId = detail.GoodsUnitId,
                GoodsUnitNameSnapshot = detail.GoodsUnitNameSnapshot, SampleQuantity = quantity,
                BatchNoSnapshot = detail.BatchNo, Conclusion = input.Conclusion, Remark = Normalize(input.Remark)
            });
        }
        return goods;
    }

    private static List<InspectionAttachment> BuildAttachments(IEnumerable<SaveInspectionAttachmentDto> inputs)
    {
        return inputs.Select(x => new InspectionAttachment
        {
            Id = Guid.NewGuid(), AttachmentType = x.AttachmentType, FileName = x.FileName.Trim(),
            FileUrl = x.FileUrl.Trim(), FileSize = x.FileSize, Sort = x.Sort
        }).ToList();
    }

    private async Task<string> GenerateInspectionNoAsync()
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var number = $"IR{DateTime.UtcNow:yyyyMMddHHmmssfff}{Guid.NewGuid():N}"[..40].ToUpperInvariant();
            if (!await inspectionReportRepository.ExistsInspectionNoAsync(number)) return number;
        }
        throw new BusinessException("检测报告编号生成失败，请重试");
    }

    private async Task<string> GenerateTraceNoAsync()
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var number = $"TR{DateTime.UtcNow:yyyyMMddHHmmssfff}{Guid.NewGuid():N}"[..40].ToUpperInvariant();
            if (!await traceRecordRepository.ExistsAsync(x => x.TraceNo == number)) return number;
        }
        throw new BusinessException("溯源编号生成失败，请重试");
    }

    private async Task ValidateAsync(SaveInspectionReportDto dto)
    {
        var result = await reportValidator.ValidateAsync(dto);
        if (!result.IsValid) throw new ValidationException(result.Errors);
    }

    private void ApplyCreateAudit(BaseEntity entity)
    {
        entity.CreateBy = currentUserService.GetUserId();
        entity.CreateName = currentUserService.GetUserName();
    }

    private void ApplyUpdateAudit(BaseEntity entity)
    {
        entity.UpdateBy = currentUserService.GetUserId();
        entity.UpdateName = currentUserService.GetUserName();
    }

    private static PagedResult<T> ToPaged<T>(IEnumerable<T> records, int total, PagedQueryParameters parameters) => new()
    {
        Records = records.ToList(), Total = total, Current = parameters.Current, Size = parameters.Size
    };

    private static InspectionReportDto ToDto(InspectionReport report) => new()
    {
        Id = report.Id, InspectionNo = report.InspectionNo, StockInOrderId = report.StockInOrderId, InNo = report.InNoSnapshot,
        WareId = report.WareId, WareName = report.WareNameSnapshot, SupplierId = report.SupplierId,
        SupplierName = report.SupplierNameSnapshot, InspectionOrg = report.InspectionOrg, SampleTime = report.SampleTime,
        InspectTime = report.InspectTime, Conclusion = report.Conclusion, Remark = report.Remark, CreateTime = report.CreateTime,
        Goods = report.Goods.OrderBy(x => x.StockInDetailId).Select(x => new InspectionReportGoodsDto
        {
            Id = x.Id, StockInDetailId = x.StockInDetailId, GoodsId = x.GoodsId, GoodsName = x.GoodsNameSnapshot,
            GoodsCode = x.GoodsCodeSnapshot, GoodsTypeName = x.GoodsTypeNameSnapshot, GoodsUnitId = x.GoodsUnitId,
            GoodsUnitName = x.GoodsUnitNameSnapshot, SampleQuantity = x.SampleQuantity, BatchNo = x.BatchNoSnapshot,
            Conclusion = x.Conclusion, Remark = x.Remark
        }).ToList(),
        Attachments = report.Attachments.OrderBy(x => x.Sort).ThenBy(x => x.Id).Select(x => new InspectionAttachmentDto
        {
            Id = x.Id, AttachmentType = x.AttachmentType, FileName = x.FileName, FileUrl = x.FileUrl,
            FileSize = x.FileSize, Sort = x.Sort
        }).ToList()
    };

    private static TraceRecordDto ToDto(TraceRecord trace) => new()
    {
        Id = trace.Id, TraceNo = trace.TraceNo, SaleOrderId = trace.SaleOrderId, SaleOrderNo = trace.SaleOrderNoSnapshot,
        SaleOrderDetailId = trace.SaleOrderDetailId, CustomerName = trace.CustomerNameSnapshot, GoodsName = trace.GoodsNameSnapshot,
        GoodsCode = trace.GoodsCodeSnapshot, GoodsTypeName = trace.GoodsTypeNameSnapshot, SupplierName = trace.SupplierNameSnapshot,
        WareName = trace.WareNameSnapshot, BatchNo = trace.BatchNoSnapshot, InspectionReportId = trace.InspectionReportId,
        Remark = trace.Remark, CreateTime = trace.CreateTime
    };

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
