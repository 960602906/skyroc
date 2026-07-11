using System.Text.Json;
using System.Text.RegularExpressions;
using Application.DTOs.Printing;
using Application.Exceptions;
using Application.interfaces;
using Domain.Entities;
using Domain.Entities.Finance;
using Domain.Entities.Orders;
using Domain.Entities.Printing;
using Domain.Entities.Purchases;
using Domain.Entities.Storage;
using Domain.Interfaces;
using Shared.Common;
using Shared.Constants;

namespace Application.Services.Printing;

/// <summary>
/// 打印应用服务，持久化模板设计与字段定义，并按既有业务快照生成不含渲染逻辑的打印数据。
/// </summary>
public class PrintService(
    IPrintTemplateRepository printTemplateRepository,
    ISaleOrderRepository saleOrderRepository,
    IPurchaseOrderRepository purchaseOrderRepository,
    IStockInOrderRepository stockInOrderRepository,
    IStockOutOrderRepository stockOutOrderRepository,
    ICustomerSettlementRepository customerSettlementRepository,
    ISupplierSettlementRepository supplierSettlementRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService) : IPrintService
{
    private const int MaxBatchSize = 100;
    private static readonly Regex TemplateCodePattern = new("^[A-Z][A-Z0-9_]{0,99}$", RegexOptions.Compiled);

    /// <inheritdoc />
    public async Task<PagedResult<PrintTemplateDto>> GetTemplatesAsync(int pageNumber, int pageSize)
    {
        if (pageNumber < 1 || pageSize is < 1 or > MaxBatchSize)
        {
            throw new BusinessException("模板分页参数不合法，页码从 1 开始且每页最多 100 条");
        }

        var (data, total) = await printTemplateRepository.GetPagedWithFieldsAsync(pageNumber, pageSize);
        return new PagedResult<PrintTemplateDto>
        {
            Records = data.Select(MapTemplate).ToList(),
            Total = total,
            Current = pageNumber,
            Size = pageSize
        };
    }

    /// <inheritdoc />
    public async Task<PrintTemplateDto> GetTemplateByCodeAsync(string templateCode)
    {
        var normalizedCode = NormalizeRequired(templateCode, "模板编码");
        var template = await printTemplateRepository.GetByCodeAsync(normalizedCode)
                       ?? throw new NotFoundException("打印模板不存在");
        return MapTemplate(template);
    }

    /// <inheritdoc />
    public async Task<PrintTemplateDto> CreateTemplateAsync(CreatePrintTemplateDto dto)
    {
        var snapshot = BuildTemplateSnapshot(dto);
        if (await printTemplateRepository.ExistsTemplateCodeAsync(snapshot.TemplateCode))
        {
            throw new BusinessException("打印模板编码已存在");
        }

        var template = new PrintTemplate
        {
            Id = Guid.NewGuid(),
            TemplateCode = snapshot.TemplateCode,
            Name = snapshot.Name,
            BusinessType = snapshot.BusinessType,
            DesignJson = snapshot.DesignJson,
            IsEnabled = snapshot.IsEnabled,
            Fields = BuildFields(snapshot.Fields),
            CreateBy = currentUserService.GetUserId(),
            CreateName = currentUserService.GetUserName()
        };
        foreach (var field in template.Fields)
        {
            field.PrintTemplateId = template.Id;
            field.CreateBy = template.CreateBy;
            field.CreateName = template.CreateName;
        }

        await printTemplateRepository.AddAsync(template);
        try
        {
            await unitOfWork.SaveChangesAsync();
        }
        catch (Exception exception) when (exception.GetType().Name == "DbUpdateException")
        {
            throw new BusinessException("打印模板编码已存在");
        }
        return MapTemplate(template);
    }

    /// <inheritdoc />
    public async Task<PrintTemplateDto> UpdateTemplateAsync(UpdatePrintTemplateDto dto)
    {
        if (dto.Id == Guid.Empty)
        {
            throw new BusinessException("打印模板主键不能为空");
        }

        var snapshot = BuildTemplateSnapshot(dto);
        try
        {
            await unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var template = await printTemplateRepository.GetWithFieldsAsync(dto.Id)
                               ?? throw new NotFoundException("打印模板不存在");
                if (await printTemplateRepository.ExistsTemplateCodeAsync(snapshot.TemplateCode, template.Id))
                {
                    throw new BusinessException("打印模板编码已存在");
                }

                template.TemplateCode = snapshot.TemplateCode;
                template.Name = snapshot.Name;
                template.BusinessType = snapshot.BusinessType;
                template.DesignJson = snapshot.DesignJson;
                template.IsEnabled = snapshot.IsEnabled;

                var originalFields = template.Fields.ToList();
                await printTemplateRepository.RemoveFieldsAsync(originalFields);
                // 先在同一事务内落库删除，避免相同 FieldKey/DisplayOrder 的替换行先插入而触发唯一约束。
                await unitOfWork.SaveChangesAsync();

                var replacement = BuildFields(snapshot.Fields);
                foreach (var field in replacement)
                {
                    field.PrintTemplateId = template.Id;
                    field.CreateBy = currentUserService.GetUserId();
                    field.CreateName = currentUserService.GetUserName();
                }

                await printTemplateRepository.AddFieldsAsync(replacement);
                template.UpdateBy = currentUserService.GetUserId();
                template.UpdateName = currentUserService.GetUserName();
            });
        }
        catch (Exception exception) when (exception.GetType().Name == "DbUpdateException")
        {
            throw new BusinessException("打印模板编码已存在");
        }

        return MapTemplate(await printTemplateRepository.GetWithFieldsAsync(dto.Id)
                           ?? throw new NotFoundException("打印模板不存在"));
    }

    /// <inheritdoc />
    public async Task<bool> DeleteTemplateAsync(Guid id)
    {
        var template = await printTemplateRepository.GetWithFieldsAsync(id)
                       ?? throw new NotFoundException("打印模板不存在");
        await printTemplateRepository.DeleteAsync(template);
        await unitOfWork.SaveChangesAsync();
        return true;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PrintDocumentDto>> GetDataAsync(
        PrintBusinessType businessType,
        IReadOnlyCollection<Guid> ids)
    {
        var orderedIds = ValidateIds(ids);
        return businessType switch
        {
            PrintBusinessType.SaleOrder => ReorderAndMap(
                orderedIds,
                await saleOrderRepository.GetByIdsAsync(orderedIds),
                order => order.Id,
                MapSaleOrder),
            PrintBusinessType.PurchaseOrder => ReorderAndMap(
                orderedIds,
                await purchaseOrderRepository.GetByIdsAsync(orderedIds),
                order => order.Id,
                MapPurchaseOrder),
            PrintBusinessType.StockInOrder => ReorderAndMap(
                orderedIds,
                await stockInOrderRepository.GetByIdsAsync(orderedIds),
                order => order.Id,
                MapStockInOrder),
            PrintBusinessType.StockOutOrder => ReorderAndMap(
                orderedIds,
                await stockOutOrderRepository.GetByIdsAsync(orderedIds),
                order => order.Id,
                MapStockOutOrder),
            PrintBusinessType.CustomerSettlement => ReorderAndMap(
                orderedIds,
                await customerSettlementRepository.GetByIdsAsync(orderedIds),
                settlement => settlement.Id,
                MapCustomerSettlement),
            PrintBusinessType.SupplierSettlement => ReorderAndMap(
                orderedIds,
                await supplierSettlementRepository.GetByIdsAsync(orderedIds),
                settlement => settlement.Id,
                MapSupplierSettlement),
            _ => throw new BusinessException("不支持的打印业务类型")
        };
    }

    /// <inheritdoc />
    public async Task ConfirmPrintedAsync(PrintBusinessType businessType, IReadOnlyCollection<Guid> ids)
    {
        var orderedIds = ValidateIds(ids);
        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            switch (businessType)
            {
                case PrintBusinessType.SaleOrder:
                    await MarkPrintedAsync(orderedIds, saleOrderRepository.MarkPrintedAsync);
                    break;
                case PrintBusinessType.StockInOrder:
                    await MarkPrintedAsync(orderedIds, stockInOrderRepository.MarkPrintedAsync);
                    break;
                case PrintBusinessType.StockOutOrder:
                    await MarkPrintedAsync(orderedIds, stockOutOrderRepository.MarkPrintedAsync);
                    break;
                default:
                    throw new BusinessException("当前业务类型不维护打印状态");
            }
        });
    }

    private static PrintTemplateSnapshot BuildTemplateSnapshot(CreatePrintTemplateDto dto)
    {
        if (!Enum.IsDefined(dto.BusinessType))
        {
            throw new BusinessException("打印业务类型不合法");
        }

        var templateCode = NormalizeRequired(dto.TemplateCode, "模板编码");
        if (!TemplateCodePattern.IsMatch(templateCode))
        {
            throw new BusinessException("模板编码只能包含大写字母、数字和下划线，且必须以字母开头");
        }

        var designJson = NormalizeRequired(dto.DesignJson, "模板设计 JSON");
        try
        {
            using var document = JsonDocument.Parse(designJson);
            if (document.RootElement.ValueKind is not (JsonValueKind.Object or JsonValueKind.Array))
            {
                throw new BusinessException("模板设计 JSON 必须是对象或数组");
            }
        }
        catch (JsonException)
        {
            throw new BusinessException("模板设计 JSON 格式不合法");
        }

        if (dto.Fields is null || dto.Fields.Count > MaxBatchSize || dto.Fields.Any(field => field is null))
        {
            throw new BusinessException("单个模板最多定义 100 个字段");
        }

        EnsureMaxLength(dto.Name, 100, "模板名称");
        var fields = dto.Fields.Select(field => new PrintTemplateFieldSnapshot
        {
            FieldKey = NormalizeRequired(field.FieldKey, "字段路径"),
            DisplayName = NormalizeRequired(field.DisplayName, "字段显示名称"),
            DisplayOrder = field.DisplayOrder,
            Format = Normalize(field.Format)
        }).ToList();
        foreach (var field in fields)
        {
            EnsureMaxLength(field.FieldKey, 200, "字段路径");
            EnsureMaxLength(field.DisplayName, 100, "字段显示名称");
            EnsureMaxLength(field.Format, 100, "字段格式");
            if (!AllowedFieldKeys.Contains(field.FieldKey))
            {
                throw new BusinessException("模板字段路径不是当前打印数据的公开字段");
            }
        }
        if (fields.Any(field => field.DisplayOrder < 0)
            || fields.GroupBy(field => field.FieldKey, StringComparer.Ordinal).Any(group => group.Count() > 1)
            || fields.GroupBy(field => field.DisplayOrder).Any(group => group.Count() > 1))
        {
            throw new BusinessException("模板字段路径和显示顺序必须在同一模板内唯一，且显示顺序不能小于零");
        }

        return new PrintTemplateSnapshot
        {
            TemplateCode = templateCode,
            Name = NormalizeRequired(dto.Name, "模板名称"),
            BusinessType = dto.BusinessType,
            DesignJson = designJson,
            IsEnabled = dto.IsEnabled,
            Fields = fields
        };
    }

    private static List<PrintTemplateField> BuildFields(IEnumerable<PrintTemplateFieldSnapshot> fields)
    {
        return fields.OrderBy(field => field.DisplayOrder).Select(field => new PrintTemplateField
        {
            Id = Guid.NewGuid(),
            FieldKey = field.FieldKey,
            DisplayName = field.DisplayName,
            DisplayOrder = field.DisplayOrder,
            Format = field.Format
        }).ToList();
    }

    private static IReadOnlyList<Guid> ValidateIds(IReadOnlyCollection<Guid> ids)
    {
        if (ids.Count is 0 or > MaxBatchSize || ids.Any(id => id == Guid.Empty) || ids.Distinct().Count() != ids.Count)
        {
            throw new BusinessException("打印单据主键必须为不重复的非空值，且单次最多 100 个");
        }

        return ids.ToArray();
    }

    private static IReadOnlyList<PrintDocumentDto> ReorderAndMap<TEntity>(
        IReadOnlyList<Guid> orderedIds,
        IReadOnlyList<TEntity> entities,
        Func<TEntity, Guid> getId,
        Func<TEntity, PrintDocumentDto> map)
    {
        if (entities.Count != orderedIds.Count)
        {
            throw new NotFoundException("打印对象不存在");
        }

        var byId = entities.ToDictionary(getId);
        return orderedIds.Select(id => map(byId[id])).ToList();
    }

    private static PrintTemplateDto MapTemplate(PrintTemplate template)
    {
        return new PrintTemplateDto
        {
            Id = template.Id,
            TemplateCode = template.TemplateCode,
            Name = template.Name,
            BusinessType = template.BusinessType,
            DesignJson = template.DesignJson,
            IsEnabled = template.IsEnabled,
            Fields = template.Fields.OrderBy(field => field.DisplayOrder).Select(field => new PrintTemplateFieldDto
            {
                Id = field.Id,
                FieldKey = field.FieldKey,
                DisplayName = field.DisplayName,
                DisplayOrder = field.DisplayOrder,
                Format = field.Format
            }).ToList()
        };
    }

    private static PrintDocumentDto MapSaleOrder(SaleOrder order) => new()
    {
        Id = order.Id,
        BusinessType = PrintBusinessType.SaleOrder,
        DocumentNo = order.OrderNo,
        BusinessPartyName = order.CustomerNameSnapshot,
        BusinessTime = order.ReceiveDate ?? order.OutDate ?? order.OrderDate,
        TotalAmount = order.OrderPrice,
        Remark = order.Remark,
        Details = order.Details.OrderBy(detail => detail.CreateTime).ThenBy(detail => detail.Id).Select(detail => new PrintDocumentDetailDto
        {
            ItemName = detail.GoodsNameSnapshot,
            ItemCode = detail.GoodsCodeSnapshot,
            UnitName = detail.GoodsUnitNameSnapshot,
            Quantity = detail.Quantity,
            UnitPrice = detail.FixedPrice,
            TotalPrice = detail.TotalPrice,
            Remark = detail.Remark
        }).ToList()
    };

    private static PrintDocumentDto MapPurchaseOrder(PurchaseOrder order) => new()
    {
        Id = order.Id,
        BusinessType = PrintBusinessType.PurchaseOrder,
        DocumentNo = order.PurchaseNo,
        BusinessPartyName = order.SupplierNameSnapshot,
        BusinessTime = order.ReceiveTime ?? order.CreateTime ?? DateTime.UnixEpoch,
        TotalAmount = NumericPrecision.RoundMoney(order.Details.Sum(detail => detail.PurchaseTotalPrice)),
        Remark = order.Remark,
        Details = order.Details.OrderBy(detail => detail.CreateTime).ThenBy(detail => detail.Id).Select(detail => new PrintDocumentDetailDto
        {
            ItemName = detail.GoodsNameSnapshot,
            ItemCode = detail.GoodsCodeSnapshot,
            UnitName = detail.PurchaseUnitNameSnapshot,
            Quantity = detail.PurchaseQuantity,
            UnitPrice = detail.PurchasePrice,
            TotalPrice = detail.PurchaseTotalPrice,
            Remark = detail.Remark
        }).ToList()
    };

    private static PrintDocumentDto MapStockInOrder(StockInOrder order) => new()
    {
        Id = order.Id,
        BusinessType = PrintBusinessType.StockInOrder,
        DocumentNo = order.InNo,
        BusinessPartyName = order.SupplierNameSnapshot ?? order.CustomerNameSnapshot,
        BusinessTime = order.InTime,
        TotalAmount = order.TotalAmount,
        Remark = order.Remark,
        Details = order.Details.OrderBy(detail => detail.CreateTime).ThenBy(detail => detail.Id).Select(detail => new PrintDocumentDetailDto
        {
            ItemName = detail.GoodsNameSnapshot,
            ItemCode = detail.GoodsCodeSnapshot,
            UnitName = detail.GoodsUnitNameSnapshot,
            Quantity = detail.Quantity,
            UnitPrice = detail.UnitPrice,
            TotalPrice = detail.TotalPrice,
            Remark = detail.Remark
        }).ToList()
    };

    private static PrintDocumentDto MapStockOutOrder(StockOutOrder order) => new()
    {
        Id = order.Id,
        BusinessType = PrintBusinessType.StockOutOrder,
        DocumentNo = order.OutNo,
        BusinessPartyName = order.CustomerNameSnapshot ?? order.SupplierNameSnapshot,
        BusinessTime = order.OutTime,
        TotalAmount = order.TotalAmount,
        Remark = order.Remark,
        Details = order.Details.OrderBy(detail => detail.CreateTime).ThenBy(detail => detail.Id).Select(detail => new PrintDocumentDetailDto
        {
            ItemName = detail.GoodsNameSnapshot,
            ItemCode = detail.GoodsCodeSnapshot,
            UnitName = detail.GoodsUnitNameSnapshot,
            Quantity = detail.Quantity,
            UnitPrice = detail.UnitPrice,
            TotalPrice = detail.TotalPrice,
            Remark = detail.Remark
        }).ToList()
    };

    private static PrintDocumentDto MapCustomerSettlement(CustomerSettlement settlement) => new()
    {
        Id = settlement.Id,
        BusinessType = PrintBusinessType.CustomerSettlement,
        DocumentNo = settlement.SettlementNo,
        BusinessPartyName = settlement.CustomerNameSnapshot,
        BusinessTime = settlement.SettlementDate,
        TotalAmount = settlement.PaymentAmount,
        Remark = settlement.Remark,
        Details = settlement.Details.OrderBy(detail => detail.CreateTime).ThenBy(detail => detail.Id).Select(detail => new PrintDocumentDetailDto
        {
            ItemName = "客户账单",
            ItemCode = detail.CustomerBillNoSnapshot,
            UnitName = "笔",
            Quantity = 1m,
            UnitPrice = detail.AppliedAmount,
            TotalPrice = detail.AppliedAmount,
            Remark = detail.Remark
        }).ToList()
    };

    private static PrintDocumentDto MapSupplierSettlement(SupplierSettlement settlement) => new()
    {
        Id = settlement.Id,
        BusinessType = PrintBusinessType.SupplierSettlement,
        DocumentNo = settlement.SettlementNo,
        BusinessPartyName = settlement.SupplierNameSnapshot,
        BusinessTime = settlement.SettlementDate,
        TotalAmount = settlement.PaymentAmount,
        Remark = settlement.Remark,
        Details = settlement.Details.OrderBy(detail => detail.CreateTime).ThenBy(detail => detail.Id).Select(detail => new PrintDocumentDetailDto
        {
            ItemName = "供应商待结单据",
            ItemCode = detail.SourceDocumentNoSnapshot,
            UnitName = "笔",
            Quantity = 1m,
            UnitPrice = detail.AppliedAmount,
            TotalPrice = detail.AppliedAmount,
            Remark = detail.Remark
        }).ToList()
    };

    private async Task MarkPrintedAsync(
        IReadOnlyCollection<Guid> ids,
        Func<IReadOnlyCollection<Guid>, Guid?, string?, Task<int>> markPrinted)
    {
        var affected = await markPrinted(ids, currentUserService.GetUserId(), currentUserService.GetUserName());
        if (affected != ids.Count)
        {
            throw new NotFoundException("打印对象不存在");
        }
    }

    private static string NormalizeRequired(string? value, string displayName)
    {
        return Normalize(value) ?? throw new BusinessException($"{displayName}不能为空");
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void EnsureMaxLength(string? value, int maxLength, string displayName)
    {
        if (value is not null && value.Length > maxLength)
        {
            throw new BusinessException($"{displayName}长度不能超过 {maxLength} 个字符");
        }
    }

    private static readonly IReadOnlySet<string> AllowedFieldKeys = new HashSet<string>(StringComparer.Ordinal)
    {
        "documentNo", "businessPartyName", "businessTime", "totalAmount", "remark",
        "details[].itemName", "details[].itemCode", "details[].unitName", "details[].quantity",
        "details[].unitPrice", "details[].totalPrice", "details[].remark"
    };

    private sealed class PrintTemplateSnapshot
    {
        public string TemplateCode { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public PrintBusinessType BusinessType { get; init; }
        public string DesignJson { get; init; } = string.Empty;
        public bool IsEnabled { get; init; }
        public List<PrintTemplateFieldSnapshot> Fields { get; init; } = [];
    }

    private sealed class PrintTemplateFieldSnapshot
    {
        public string FieldKey { get; init; } = string.Empty;
        public string DisplayName { get; init; } = string.Empty;
        public int DisplayOrder { get; init; }
        public string? Format { get; init; }
    }
}
