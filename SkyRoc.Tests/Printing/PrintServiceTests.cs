using Application.DTOs.Printing;
using Application.Exceptions;
using Application.Services.Printing;
using Domain.Entities.Orders;
using Domain.Entities.Printing;
using Domain.Entities.Purchases;
using Domain.Entities.Storage;
using Domain.Entities.Finance;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace SkyRoc.Tests.Printing;

/// <summary>
/// 打印模板和业务打印数据服务的回归测试。
/// </summary>
public class PrintServiceTests
{
    [Fact]
    public async Task CreateTemplate_PersistsFieldsAndRejectsDuplicateCode()
    {
        await using var context = CreateDbContext();
        var service = CreateService(context);
        var request = new CreatePrintTemplateDto
        {
            TemplateCode = "CUSTOMER_ORDER_DELIVERY",
            Name = "客户配送单",
            BusinessType = PrintBusinessType.SaleOrder,
            DesignJson = "{\"version\":\"1\"}",
            Fields =
            [
                new PrintTemplateFieldInputDto { FieldKey = "documentNo", DisplayName = "订单号", DisplayOrder = 1 },
                new PrintTemplateFieldInputDto { FieldKey = "details[].itemName", DisplayName = "商品名称", DisplayOrder = 2 }
            ]
        };

        var created = await service.CreateTemplateAsync(request);

        Assert.Equal("CUSTOMER_ORDER_DELIVERY", created.TemplateCode);
        Assert.Equal(2, created.Fields.Count);
        Assert.Equal("documentNo", created.Fields[0].FieldKey);
        await Assert.ThrowsAsync<BusinessException>(() => service.CreateTemplateAsync(request));
    }

    [Fact]
    public async Task GetTemplates_ReturnsPersistedFieldsInDisplayOrder()
    {
        await using var context = CreateDbContext();
        var service = CreateService(context);
        await service.CreateTemplateAsync(new CreatePrintTemplateDto
        {
            TemplateCode = "PURCHASE_ORDER",
            Name = "采购单",
            BusinessType = PrintBusinessType.PurchaseOrder,
            DesignJson = "{}",
            Fields =
            [
                new PrintTemplateFieldInputDto { FieldKey = "details[].itemName", DisplayName = "商品", DisplayOrder = 2 },
                new PrintTemplateFieldInputDto { FieldKey = "documentNo", DisplayName = "采购单号", DisplayOrder = 1 }
            ]
        });

        var page = await service.GetTemplatesAsync(1, 20);

        var template = Assert.Single(page.Records!);
        Assert.Equal(["documentNo", "details[].itemName"], template.Fields.Select(field => field.FieldKey));
    }

    [Fact]
    public async Task GetTemplateByCode_DisabledTemplateIsNotAvailableForBusinessPrinting()
    {
        await using var context = CreateDbContext();
        var service = CreateService(context);
        await service.CreateTemplateAsync(new CreatePrintTemplateDto
        {
            TemplateCode = "DISABLED_TEMPLATE", Name = "停用模板", BusinessType = PrintBusinessType.SaleOrder,
            DesignJson = "{}", IsEnabled = false
        });

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetTemplateByCodeAsync("DISABLED_TEMPLATE"));
    }

    [Fact]
    public async Task UpdateTemplate_ReplacesFieldsWithOverlappingKeysAndOrders()
    {
        await using var context = CreateDbContext();
        var service = CreateService(context);
        var created = await service.CreateTemplateAsync(new CreatePrintTemplateDto
        {
            TemplateCode = "SALE_ORDER_PRINT",
            Name = "销售订单打印",
            BusinessType = PrintBusinessType.SaleOrder,
            DesignJson = "{\"version\":\"1\"}",
            Fields =
            [
                new PrintTemplateFieldInputDto { FieldKey = "documentNo", DisplayName = "订单号", DisplayOrder = 0 },
                new PrintTemplateFieldInputDto { FieldKey = "details[].itemName", DisplayName = "商品", DisplayOrder = 1 }
            ]
        });

        var updated = await service.UpdateTemplateAsync(new UpdatePrintTemplateDto
        {
            Id = created.Id,
            TemplateCode = "SALE_ORDER_PRINT",
            Name = "销售订单打印（改）",
            BusinessType = PrintBusinessType.SaleOrder,
            DesignJson = "{\"version\":\"2\"}",
            Fields =
            [
                new PrintTemplateFieldInputDto { FieldKey = "documentNo", DisplayName = "单据号", DisplayOrder = 0 },
                new PrintTemplateFieldInputDto { FieldKey = "details[].itemName", DisplayName = "品名", DisplayOrder = 1 },
                new PrintTemplateFieldInputDto { FieldKey = "totalAmount", DisplayName = "合计", DisplayOrder = 2 }
            ]
        });

        Assert.Equal("销售订单打印（改）", updated.Name);
        Assert.Equal("{\"version\":\"2\"}", updated.DesignJson);
        Assert.Equal(
            [("documentNo", "单据号", 0), ("details[].itemName", "品名", 1), ("totalAmount", "合计", 2)],
            updated.Fields.Select(field => (field.FieldKey, field.DisplayName, field.DisplayOrder)));
        Assert.Equal(3, await context.Set<PrintTemplateField>().CountAsync(field => field.PrintTemplateId == created.Id));
    }

    [Fact]
    public async Task CreateTemplate_InvalidFieldAndOverlongNameThrowBusinessException()
    {
        await using var context = CreateDbContext();
        var service = CreateService(context);
        var exception = await Assert.ThrowsAsync<BusinessException>(() => service.CreateTemplateAsync(new CreatePrintTemplateDto
        {
            TemplateCode = "INVALID_TEMPLATE", Name = new string('名', 101), BusinessType = PrintBusinessType.SaleOrder,
            DesignJson = "{}", Fields = [new PrintTemplateFieldInputDto { FieldKey = "details[].goodsName", DisplayName = "错误字段", DisplayOrder = 0 }]
        }));

        Assert.Contains("模板名称长度", exception.Message);
    }

    [Fact]
    public async Task GetPrintData_SaleOrderReturnsSnapshotWithoutChangingPrintStatus()
    {
        await using var context = CreateDbContext();
        var order = new SaleOrder
        {
            Id = Guid.NewGuid(),
            OrderNo = "SO-PRINT-001",
            CustomerNameSnapshot = "测试客户",
            OrderDate = new DateTime(2026, 7, 11, 8, 0, 0, DateTimeKind.Utc),
            OrderPrice = 12.34m,
            Details =
            [
                new SaleOrderDetail
                {
                    Id = Guid.NewGuid(),
                    GoodsId = Guid.NewGuid(),
                    GoodsNameSnapshot = "番茄",
                    GoodsCodeSnapshot = "TOMATO",
                    GoodsUnitId = Guid.NewGuid(),
                    GoodsUnitNameSnapshot = "斤",
                    Quantity = 2m,
                    BaseQuantity = 2m,
                    FixedPrice = 6.17m,
                    TotalPrice = 12.34m
                }
            ]
        };
        await context.SaleOrders.AddAsync(order);
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var document = await service.GetDataAsync(PrintBusinessType.SaleOrder, [order.Id]);

        var data = Assert.Single(document);
        Assert.Equal("SO-PRINT-001", data.DocumentNo);
        Assert.Equal("测试客户", data.BusinessPartyName);
        Assert.Equal(12.34m, data.TotalAmount);
        Assert.Equal("番茄", Assert.Single(data.Details).ItemName);
        Assert.Equal(OrderPrintStatus.NotPrinted, (await context.SaleOrders.SingleAsync()).PrintStatus);
    }

    [Fact]
    public async Task ConfirmPrinted_SaleOrderMarksOnlyRequestedDocument()
    {
        await using var context = CreateDbContext();
        var requested = new SaleOrder { Id = Guid.NewGuid(), OrderNo = "SO-PRINT-002", CustomerNameSnapshot = "客户 A" };
        var untouched = new SaleOrder { Id = Guid.NewGuid(), OrderNo = "SO-PRINT-003", CustomerNameSnapshot = "客户 B" };
        await context.SaleOrders.AddRangeAsync(requested, untouched);
        await context.SaveChangesAsync();
        var service = CreateService(context);

        await service.ConfirmPrintedAsync(PrintBusinessType.SaleOrder, [requested.Id]);

        Assert.Equal(OrderPrintStatus.Printed, (await context.SaleOrders.FindAsync(requested.Id))!.PrintStatus);
        Assert.Equal(OrderPrintStatus.NotPrinted, (await context.SaleOrders.FindAsync(untouched.Id))!.PrintStatus);
    }

    [Fact]
    public async Task GetPrintData_EachSupportedBusinessTypeReturnsItsOwnSnapshot()
    {
        await using var context = CreateDbContext();
        var purchase = new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            PurchaseNo = "PO-PRINT-001",
            SupplierNameSnapshot = "供应商",
            ReceiveTime = new DateTime(2026, 7, 11, 9, 0, 0, DateTimeKind.Utc),
            Details = [new PurchaseOrderDetail
            {
                Id = Guid.NewGuid(), GoodsId = Guid.NewGuid(), GoodsNameSnapshot = "土豆", GoodsCodeSnapshot = "POTATO",
                PurchaseUnitId = Guid.NewGuid(), PurchaseUnitNameSnapshot = "袋", RequiredQuantity = 1m,
                PurchaseQuantity = 2m, PurchasePrice = 3m, PurchaseTotalPrice = 6m
            }]
        };
        var stockIn = new StockInOrder
        {
            Id = Guid.NewGuid(),
            InNo = "SI-PRINT-001",
            WareId = Guid.NewGuid(),
            WareNameSnapshot = "主仓",
            SupplierNameSnapshot = "供应商",
            InTime = new DateTime(2026, 7, 11, 10, 0, 0, DateTimeKind.Utc),
            TotalAmount = 8m,
            Details = [new StockInDetail
            {
                Id = Guid.NewGuid(), GoodsId = Guid.NewGuid(), GoodsNameSnapshot = "白菜", GoodsCodeSnapshot = "CABBAGE",
                GoodsUnitId = Guid.NewGuid(), GoodsUnitNameSnapshot = "斤", ConversionRate = 1m, Quantity = 2m,
                BaseQuantity = 2m, UnitPrice = 4m, TotalPrice = 8m, BatchNo = "B-1"
            }]
        };
        var stockOut = new StockOutOrder
        {
            Id = Guid.NewGuid(),
            OutNo = "SO-PRINT-001",
            WareId = Guid.NewGuid(),
            WareNameSnapshot = "主仓",
            CustomerNameSnapshot = "客户",
            OutTime = new DateTime(2026, 7, 11, 11, 0, 0, DateTimeKind.Utc),
            TotalAmount = 10m,
            Details = [new StockOutDetail
            {
                Id = Guid.NewGuid(), GoodsId = Guid.NewGuid(), GoodsNameSnapshot = "萝卜", GoodsCodeSnapshot = "RADISH",
                GoodsUnitId = Guid.NewGuid(), GoodsUnitNameSnapshot = "斤", ConversionRate = 1m, Quantity = 2m,
                BaseQuantity = 2m, UnitPrice = 5m, TotalPrice = 10m, BatchNoSnapshot = "B-2"
            }]
        };
        var customerSettlement = new CustomerSettlement
        {
            Id = Guid.NewGuid(),
            SettlementNo = "CS-PRINT-001",
            CustomerId = Guid.NewGuid(),
            CustomerNameSnapshot = "客户",
            SettlementDate = new DateTime(2026, 7, 11, 12, 0, 0, DateTimeKind.Utc),
            PaymentAmount = 11m,
            Details = [new CustomerSettlementDetail
            {
                Id = Guid.NewGuid(), CustomerBillId = Guid.NewGuid(), CustomerBillNoSnapshot = "CB-001", SaleOrderId = Guid.NewGuid(),
                SaleOrderNoSnapshot = "SALE-001", ReceivableAmountSnapshot = 11m, PaymentAmount = 11m, AppliedAmount = 11m
            }]
        };
        var supplierSettlement = new SupplierSettlement
        {
            Id = Guid.NewGuid(),
            SettlementNo = "SS-PRINT-001",
            SupplierId = Guid.NewGuid(),
            SupplierNameSnapshot = "供应商",
            SettlementDate = new DateTime(2026, 7, 11, 13, 0, 0, DateTimeKind.Utc),
            PaymentAmount = 12m,
            Details = [new SupplierSettlementDetail
            {
                Id = Guid.NewGuid(), SupplierBillId = Guid.NewGuid(), SupplierBillNoSnapshot = "SB-001",
                SourceDocumentNoSnapshot = "SI-001", PayableAmountSnapshot = 12m, PaymentAmount = 12m, AppliedAmount = 12m
            }]
        };
        await context.AddRangeAsync(purchase, stockIn, stockOut, customerSettlement, supplierSettlement);
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var purchaseData = Assert.Single(await service.GetDataAsync(PrintBusinessType.PurchaseOrder, [purchase.Id]));
        var stockInData = Assert.Single(await service.GetDataAsync(PrintBusinessType.StockInOrder, [stockIn.Id]));
        var stockOutData = Assert.Single(await service.GetDataAsync(PrintBusinessType.StockOutOrder, [stockOut.Id]));
        var customerSettlementData = Assert.Single(await service.GetDataAsync(PrintBusinessType.CustomerSettlement, [customerSettlement.Id]));
        var supplierSettlementData = Assert.Single(await service.GetDataAsync(PrintBusinessType.SupplierSettlement, [supplierSettlement.Id]));

        Assert.Equal(("PO-PRINT-001", 6m, "土豆"), (purchaseData.DocumentNo, purchaseData.TotalAmount, Assert.Single(purchaseData.Details).ItemName));
        Assert.Equal(("SI-PRINT-001", 8m, "白菜"), (stockInData.DocumentNo, stockInData.TotalAmount, Assert.Single(stockInData.Details).ItemName));
        Assert.Equal(("SO-PRINT-001", 10m, "萝卜"), (stockOutData.DocumentNo, stockOutData.TotalAmount, Assert.Single(stockOutData.Details).ItemName));
        Assert.Equal(("CS-PRINT-001", 11m, "CB-001"), (customerSettlementData.DocumentNo, customerSettlementData.TotalAmount, Assert.Single(customerSettlementData.Details).ItemCode));
        Assert.Equal(("SS-PRINT-001", 12m, "SI-001"), (supplierSettlementData.DocumentNo, supplierSettlementData.TotalAmount, Assert.Single(supplierSettlementData.Details).ItemCode));
    }

    [Fact]
    public void PrintTemplateModel_HasConstraintsAndAccuratePersistedComments()
    {
        using var context = CreateDbContext();
        var model = context.GetService<IDesignTimeModel>().Model;
        var template = model.FindEntityType(typeof(PrintTemplate))!;
        var field = model.FindEntityType(typeof(PrintTemplateField))!;

        Assert.Equal("打印模板，保存业务单据可复用的设计器 JSON 和启用状态", template.GetComment());
        Assert.Contains("模板名称", template.FindProperty(nameof(PrintTemplate.Name))!.GetComment());
        Assert.Contains("采购单", template.FindProperty(nameof(PrintTemplate.BusinessType))!.GetComment());
        Assert.True(template.FindIndex(template.FindProperty(nameof(PrintTemplate.TemplateCode))!)!.IsUnique);
        Assert.True(field.FindIndex([
            field.FindProperty(nameof(PrintTemplateField.PrintTemplateId))!,
            field.FindProperty(nameof(PrintTemplateField.FieldKey))!
        ])!.IsUnique);
        Assert.Contains(field.GetCheckConstraints(), constraint => constraint.Name == "ck_print_template_field_order");
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new ApplicationDbContext(options);
    }

    private static PrintService CreateService(ApplicationDbContext context)
    {
        return new PrintService(
            new PrintTemplateRepository(context),
            new SaleOrderRepository(context),
            new PurchaseOrderRepository(context),
            new StockInOrderRepository(context),
            new StockOutOrderRepository(context),
            new CustomerSettlementRepository(context),
            new SupplierSettlementRepository(context),
            new UnitOfWork(context),
            new TestCurrentUserService());
    }

    private sealed class TestCurrentUserService : Application.interfaces.ICurrentUserService
    {
        public Guid? GetUserId() => Guid.Parse("11111111-1111-1111-1111-111111111111");
        public string? GetUserName() => "print-test";
        public string? GetEmail() => null;
        public string? GetRole() => "admin";
        public IReadOnlyList<string> GetRoles() => ["admin"];
        public bool HasClaim(string claimType, string claimValue) => false;
    }
}
