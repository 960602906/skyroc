using Application.Exceptions;
using Application.Interfaces;
using Shared.Constants;

namespace Application.Services;

/// <summary>
/// 单据编号统一生成服务：前缀 + UTC(<c>yyyyMMddHHmmssfff</c>) + Guid 片段，冲突时最多重试 5 次。
/// </summary>
public class DocumentNoGeneratorService : IDocumentNoGenerator
{
    private const int MaxAttempts = 5;

    /// <inheritdoc />
    public async Task<string> NextAsync(DocumentNoKind kind, Func<string, Task<bool>> existsCheck)
    {
        ArgumentNullException.ThrowIfNull(existsCheck);
        var rule = DocumentNoRules.Get(kind);

        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            var number = CreateCandidate(rule);
            if (!await existsCheck(number))
            {
                return number;
            }
        }

        throw new BusinessException(rule.FailureMessage);
    }

    private static string CreateCandidate(DocumentNoRule rule)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
        if (rule.MaxLength is { } maxLength)
        {
            var raw = $"{rule.Prefix}{timestamp}{Guid.NewGuid():N}".ToUpperInvariant();
            return raw.Length <= maxLength ? raw : raw[..maxLength];
        }

        var suffixLength = rule.SuffixLength
                           ?? throw new InvalidOperationException($"单据编号规则 {rule.Prefix} 未配置后缀长度。");
        var suffix = Guid.NewGuid().ToString("N")[..suffixLength].ToUpperInvariant();
        return $"{rule.Prefix}{timestamp}{suffix}";
    }

    /// <summary>
    /// 单种单据编号的前缀、长度策略与失败文案。
    /// </summary>
    /// <param name="Prefix">业务前缀。</param>
    /// <param name="FailureMessage">重试耗尽时的业务异常文案。</param>
    /// <param name="SuffixLength">Guid 截取长度；与 <paramref name="MaxLength"/> 二选一。</param>
    /// <param name="MaxLength">整串截断上限；与 <paramref name="SuffixLength"/> 二选一。</param>
    private sealed record DocumentNoRule(
        string Prefix,
        string FailureMessage,
        int? SuffixLength = null,
        int? MaxLength = null);

    /// <summary>
    /// 集中维护各 <see cref="DocumentNoKind"/> 的发号规则。
    /// </summary>
    private static class DocumentNoRules
    {
        public static DocumentNoRule Get(DocumentNoKind kind) => kind switch
        {
            DocumentNoKind.SaleOrder => new("SO", "订单号生成失败，请重试", SuffixLength: 12),
            DocumentNoKind.PurchasePlan => new("PP", "采购计划编号生成失败，请重试", SuffixLength: 12),
            DocumentNoKind.PurchaseOrder => new("PO", "采购单编号生成失败，请重试", SuffixLength: 12),
            DocumentNoKind.StockIn => new("IN", "入库单编号生成失败，请重试", SuffixLength: 12),
            DocumentNoKind.StockOut => new("OUT", "出库单编号生成失败，请重试", SuffixLength: 12),
            DocumentNoKind.Stocktaking => new("STK", "盘点单编号生成失败，请重试", SuffixLength: 12),
            DocumentNoKind.DeliveryTask => new("DT", "配送任务编号生成失败，请重试", SuffixLength: 10),
            DocumentNoKind.OrderReceipt => new("OR", "签收回单编号生成失败，请重试", SuffixLength: 10),
            DocumentNoKind.DeliveryException => new("DE", "配送异常编号生成失败，请重试", SuffixLength: 10),
            DocumentNoKind.AfterSale => new("AS", "售后单号生成失败，请重试", SuffixLength: 12),
            DocumentNoKind.PickupTask => new("PU", "取货任务编号生成失败，请重试", MaxLength: 42),
            DocumentNoKind.CustomerBill => new("CB", "客户账单编号生成失败，请重试", SuffixLength: 10),
            DocumentNoKind.SupplierBill => new("SB", "供应商待结单据编号生成失败，请重试", SuffixLength: 10),
            DocumentNoKind.CustomerSettlement => new("CS", "客户结款凭证编号生成失败，请重试", SuffixLength: 10),
            DocumentNoKind.SupplierSettlement => new("SS", "供应商结算单编号生成失败，请重试", SuffixLength: 10),
            DocumentNoKind.InspectionReport => new("IR", "检测报告编号生成失败，请重试", MaxLength: 40),
            DocumentNoKind.TraceRecord => new("TR", "溯源编号生成失败，请重试", MaxLength: 40),
            DocumentNoKind.ImportExportJob => new("IE", "导入导出任务编号生成失败，请重试", MaxLength: 48),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "不支持的单据编号种类。")
        };
    }
}
