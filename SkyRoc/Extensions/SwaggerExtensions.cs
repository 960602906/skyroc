using System.Reflection;
using Application.DTOs;
using Domain.Entities.Orders;
using Microsoft.OpenApi.Models;
using Shared.Common;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SkyRoc.Extensions;

/// <summary>
///     Swagger 配置扩展。
/// </summary>
public static class SwaggerExtensions
{
    private static readonly Dictionary<string, string> ControllerTags = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Auth"] = "认证",
        ["Profile"] = "认证",
        ["Users"] = "系统权限",
        ["Roles"] = "系统权限",
        ["Menus"] = "系统权限",
        ["MenuButtons"] = "系统权限",
        ["Departments"] = "系统权限",
        ["Goods"] = "基础资料",
        ["GoodsTypes"] = "基础资料",
        ["GoodsUnits"] = "基础资料",
        ["Customers"] = "基础资料",
        ["CustomerTags"] = "基础资料",
        ["CustomerSubAccounts"] = "基础资料",
        ["Companies"] = "基础资料",
        ["Suppliers"] = "基础资料",
        ["Purchasers"] = "基础资料",
        ["Wares"] = "基础资料",
        ["PurchaseRules"] = "基础资料",
        ["Quotations"] = "定价",
        ["QuotationGoods"] = "定价",
        ["CustomerProtocols"] = "定价",
        ["CustomerProtocolGoods"] = "定价",
        ["Orders"] = "销售订单",
        ["PurchasePlans"] = "采购",
        ["PurchaseOrders"] = "采购",
        ["PurchaseStockIn"] = "库存",
        ["OtherStockIn"] = "库存",
        ["SalesReturnStockIn"] = "库存",
        ["SaleStockOut"] = "库存",
        ["PurchaseReturnStockOut"] = "库存",
        ["OtherStockOut"] = "库存",
        ["Stocktaking"] = "库存",
        ["StockQuery"] = "库存",
        ["Drivers"] = "配送",
        ["Carriers"] = "配送",
        ["Routes"] = "配送",
        ["DeliveryTasks"] = "配送",
        ["DeliveryExceptions"] = "配送",
        ["AfterSales"] = "售后",
        ["PickupTasks"] = "售后",
        ["CustomerSettlements"] = "财务",
        ["SupplierSettlements"] = "财务",
        ["InspectionReports"] = "溯源",
        ["TraceRecords"] = "溯源",
        ["ExternalPushLogs"] = "溯源",
        ["Reports"] = "报表",
        ["Dashboard"] = "首页驾驶舱",
        ["ImportExportJobs"] = "导入导出",
        ["Files"] = "文件上传",
        ["PrintTemplates"] = "打印",
        ["PrintData"] = "打印",
        ["SystemSettings"] = "系统支撑",
        ["Notices"] = "系统支撑",
        ["Logs"] = "系统支撑"
    };

    /// <summary>
    ///     添加 Swagger 文档生成服务。
    /// </summary>
    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "SkyRoc API",
                Version = "v1",
                Description =
                    "SkyRoc 生鲜供应链后台 API。覆盖认证与系统权限、基础资料、定价、销售订单、采购、库存、配送与售后等模块。" +
                    "所有成功响应均包装为 `{ code, msg, data }`，其中 `code=200` 表示成功。"
            });

            IncludeXmlCommentsFromAssemblies(
                options,
                typeof(Program).Assembly,
                typeof(BaseDto).Assembly,
                typeof(SaleOrder).Assembly,
                typeof(ApiResponse<>).Assembly);

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                Description = "输入登录接口返回的 AccessToken。"
            });

            options.TagActionsBy(api =>
            {
                if (!api.ActionDescriptor.RouteValues.TryGetValue("controller", out var controller)
                    || string.IsNullOrWhiteSpace(controller))
                    return ["其他"];

                var tag = ControllerTags.TryGetValue(controller, out var mapped) ? mapped : "其他";
                return [tag];
            });

            options.SchemaFilter<SwaggerDateTimeSchemaFilter>();
            options.SchemaFilter<SwaggerEnumSchemaFilter>();
            options.OperationFilter<SwaggerAuthorizationOperationFilter>();
        });

        return services;
    }

    private static void IncludeXmlCommentsFromAssemblies(SwaggerGenOptions options, params Assembly[] assemblies)
    {
        foreach (var assembly in assemblies.Distinct())
        {
            var xmlFile = $"{assembly.GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
                options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
        }
    }
}
