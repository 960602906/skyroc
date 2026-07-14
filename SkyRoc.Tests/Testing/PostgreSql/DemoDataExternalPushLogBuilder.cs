using System.Text.Json;
using Domain.Entities.Traceability;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace SkyRoc.Tests.Testing.PostgreSql;

/// <summary>
///     以脱敏请求中的完整稳定键补齐只追加外部报送日志，并在复用前校验来源、状态和审计指纹。
/// </summary>
internal sealed class DemoDataExternalPushLogBuilder(
    ApplicationDbContext context,
    Guid auditUserId,
    string auditUsername)
{
    private const int LogCount = 120;
    private const int SourceCountPerBusinessType = 40;
    private const string LogArea = "EXTERNAL-PUSH-LOG";
    private const string ManagedRequestPrefix = "{\"demoKey\":\"SKYROC-DEMO-EXTERNAL-PUSH-LOG-";

    /// <summary>
    ///     补齐一百二十条外部报送日志，均衡覆盖三类真实来源和待报送、成功、失败状态。
    /// </summary>
    public async Task<DemoDataExternalPushLogGenerationResult> GenerateAsync(
        CancellationToken cancellationToken)
    {
        var sources = await LoadManagedSourcesAsync(cancellationToken);
        var seeds = Enumerable.Range(1, LogCount)
            .Select(sequence => CreateSeed(sequence, sources))
            .ToArray();
        var expectedStableKeys = seeds
            .Select(seed => seed.StableKey)
            .ToHashSet(StringComparer.Ordinal);
        var candidates = await context.ExternalPushLogs
            .Where(log => log.RequestContent != null
                          && log.RequestContent.StartsWith(ManagedRequestPrefix))
            .OrderBy(log => log.RequestContent)
            .ToListAsync(cancellationToken);
        var candidateKeys = candidates
            .Select(log => ReadStableKey(log.RequestContent!))
            .ToArray();

        EnsureOnlyExpectedStableKeys(candidateKeys, expectedStableKeys);
        EnsureNoDuplicateStableKeys(candidateKeys);
        var logsByStableKey = candidates
            .Zip(candidateKeys)
            .ToDictionary(pair => pair.Second, pair => pair.First, StringComparer.Ordinal);
        var createdLogs = 0;
        var reusedLogs = 0;

        foreach (var seed in seeds)
        {
            if (!logsByStableKey.TryGetValue(seed.StableKey, out var log))
            {
                log = seed.ToEntity(auditUserId, auditUsername);
                await context.ExternalPushLogs.AddAsync(log, cancellationToken);
                logsByStableKey.Add(seed.StableKey, log);
                createdLogs++;
                continue;
            }

            seed.Validate(log, auditUserId, auditUsername);
            reusedLogs++;
        }

        await context.SaveChangesAsync(cancellationToken);
        return new DemoDataExternalPushLogGenerationResult(createdLogs, reusedLogs);
    }

    private async Task<IReadOnlyList<ExternalPushSource>> LoadManagedSourcesAsync(
        CancellationToken cancellationToken)
    {
        var saleOrderKeys = Enumerable.Range(1, SourceCountPerBusinessType)
            .Select(sequence => DemoDataStableKeyCatalog.Create("SALE-ORDER", sequence))
            .ToArray();
        var inspectionRemarks = Enumerable.Range(1, SourceCountPerBusinessType)
            .Select(CreateInspectionReportRemark)
            .ToArray();
        var traceRemarks = Enumerable.Range(1, SourceCountPerBusinessType / 2)
            .Select(CreateTraceRecordRemark)
            .ToArray();

        var saleOrders = await context.SaleOrders
            .AsNoTracking()
            .Where(order => order.InnerRemark != null && saleOrderKeys.Contains(order.InnerRemark))
            .OrderBy(order => order.InnerRemark)
            .Select(order => new { order.Id, order.OrderNo, order.InnerRemark })
            .ToArrayAsync(cancellationToken);
        var inspectionReports = await context.InspectionReports
            .AsNoTracking()
            .Where(report => report.Remark != null && inspectionRemarks.Contains(report.Remark))
            .OrderBy(report => report.Remark)
            .Select(report => new { report.Id, report.InspectionNo, report.Remark })
            .ToArrayAsync(cancellationToken);
        var traceRecords = await context.TraceRecords
            .AsNoTracking()
            .Where(trace => trace.Remark != null && traceRemarks.Contains(trace.Remark))
            .OrderBy(trace => trace.Remark)
            .Select(trace => new { trace.Id, trace.TraceNo, trace.Remark })
            .ToArrayAsync(cancellationToken);

        if (saleOrders.Length != SourceCountPerBusinessType
            || inspectionReports.Length != SourceCountPerBusinessType
            || traceRecords.Length != SourceCountPerBusinessType / 2)
        {
            throw new InvalidOperationException(
                $"外部报送日志需要 {SourceCountPerBusinessType} 张销售订单、{SourceCountPerBusinessType} 张检测报告和 {SourceCountPerBusinessType / 2} 条溯源记录；当前分别为 {saleOrders.Length}、{inspectionReports.Length}、{traceRecords.Length}。");
        }

        for (var index = 0; index < SourceCountPerBusinessType; index++)
        {
            if (!string.Equals(saleOrders[index].InnerRemark, saleOrderKeys[index], StringComparison.Ordinal)
                || string.IsNullOrWhiteSpace(saleOrders[index].OrderNo)
                || !string.Equals(inspectionReports[index].Remark, inspectionRemarks[index], StringComparison.Ordinal)
                || string.IsNullOrWhiteSpace(inspectionReports[index].InspectionNo))
            {
                throw new InvalidOperationException("受管销售订单或检测报告的稳定来源键与编号快照已漂移。");
            }
        }

        for (var index = 0; index < traceRecords.Length; index++)
        {
            if (!string.Equals(traceRecords[index].Remark, traceRemarks[index], StringComparison.Ordinal)
                || string.IsNullOrWhiteSpace(traceRecords[index].TraceNo))
            {
                throw new InvalidOperationException("受管溯源记录的稳定来源键与编号快照已漂移。");
            }
        }

        return saleOrders
            .Select(order => new ExternalPushSource(
                ExternalPushBusinessType.SaleOrder,
                order.Id,
                order.OrderNo))
            .Concat(inspectionReports.Select(report => new ExternalPushSource(
                ExternalPushBusinessType.InspectionReport,
                report.Id,
                report.InspectionNo)))
            .Concat(traceRecords.Select(trace => new ExternalPushSource(
                ExternalPushBusinessType.TraceRecord,
                trace.Id,
                trace.TraceNo)))
            .Concat(traceRecords.Select(trace => new ExternalPushSource(
                ExternalPushBusinessType.TraceRecord,
                trace.Id,
                trace.TraceNo)))
            .ToArray();
    }

    private static ExternalPushLogSeed CreateSeed(
        int sequence,
        IReadOnlyList<ExternalPushSource> sources)
    {
        var source = sources[sequence - 1];
        var stableKey = DemoDataStableKeyCatalog.Create(LogArea, sequence);
        var pushStatus = ((sequence - 1) % 3) switch
        {
            0 => ExternalPushStatus.Pending,
            1 => ExternalPushStatus.Success,
            _ => ExternalPushStatus.Failed
        };
        var platformCode = ((sequence - 1) % 3) switch
        {
            0 => "EAST-AGRI-TRACE",
            1 => "CITY-MARKET-SUPERVISION",
            _ => "QUALITY-CERT-HUB"
        };
        var pushTime = new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc)
            .AddHours(sequence * 3);
        DateTime? responseTime = pushStatus == ExternalPushStatus.Pending
            ? null
            : pushTime.AddSeconds(12 + sequence % 17);
        var requestContent = JsonSerializer.Serialize(new
        {
            demoKey = stableKey,
            businessType = (int)source.BusinessType,
            businessNo = source.BusinessNo,
            regionCode = "CN-EAST-FRESH"
        });
        var responseContent = pushStatus switch
        {
            ExternalPushStatus.Pending => null,
            ExternalPushStatus.Success => JsonSerializer.Serialize(new
            {
                accepted = true,
                externalReceiptNo = $"EXT-RECEIPT-{sequence:D3}"
            }),
            ExternalPushStatus.Failed => JsonSerializer.Serialize(new
            {
                accepted = false,
                errorCode = "BUSINESS_VALIDATION_REJECTED"
            }),
            _ => throw new InvalidOperationException($"未覆盖的外部报送状态：{pushStatus}。")
        };
        var retryCount = source.BusinessType == ExternalPushBusinessType.TraceRecord
            ? (sequence - 81) / 20
            : (sequence - 1) % 3;

        return new ExternalPushLogSeed(
            stableKey,
            source.BusinessType,
            source.BusinessId,
            source.BusinessNo,
            platformCode,
            pushStatus,
            pushTime,
            responseTime,
            requestContent,
            responseContent,
            pushStatus == ExternalPushStatus.Failed
                ? "外部平台返回业务校验错误；可按来源编号复核后重试。"
                : null,
            retryCount);
    }

    private static string ReadStableKey(string requestContent)
    {
        try
        {
            using var document = JsonDocument.Parse(requestContent);
            if (document.RootElement.TryGetProperty("demoKey", out var property)
                && property.ValueKind == JsonValueKind.String
                && !string.IsNullOrWhiteSpace(property.GetString()))
            {
                return property.GetString()!;
            }
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException("受管外部报送日志请求快照不是合法 JSON。", exception);
        }

        throw new InvalidOperationException("受管外部报送日志请求快照缺少完整 demoKey。");
    }

    private static void EnsureOnlyExpectedStableKeys(
        IEnumerable<string> actualKeys,
        IReadOnlySet<string> expectedKeys)
    {
        var unknownKeys = actualKeys
            .Where(key => !expectedKeys.Contains(key))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        if (unknownKeys.Length > 0)
        {
            throw new InvalidOperationException(
                $"检测到未知的受管外部报送日志稳定键：{string.Join("、", unknownKeys)}。");
        }
    }

    private static void EnsureNoDuplicateStableKeys(IEnumerable<string> actualKeys)
    {
        var duplicateKeys = actualKeys
            .GroupBy(key => key, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .Order(StringComparer.Ordinal)
            .ToArray();
        if (duplicateKeys.Length > 0)
        {
            throw new InvalidOperationException(
                $"检测到重复的受管外部报送日志稳定键：{string.Join("、", duplicateKeys)}。");
        }
    }

    private static string CreateInspectionReportRemark(int sequence)
    {
        var stableKey = DemoDataStableKeyCatalog.Create("INSPECTION-REPORT", sequence);
        return $"{stableKey} 华东联调检测报告{sequence:D2}：记录受管采购入库商品抽检结论与附件快照。";
    }

    private static string CreateTraceRecordRemark(int sequence)
    {
        var stableKey = DemoDataStableKeyCatalog.Create("TRACE-RECORD", sequence);
        return $"{stableKey} 华东联调溯源记录{sequence:D2}：串联销售商品、采购批次与检测报告。";
    }

    private sealed record ExternalPushSource(
        ExternalPushBusinessType BusinessType,
        Guid BusinessId,
        string BusinessNo);

    private sealed record ExternalPushLogSeed(
        string StableKey,
        ExternalPushBusinessType BusinessType,
        Guid BusinessId,
        string BusinessNoSnapshot,
        string PlatformCode,
        ExternalPushStatus PushStatus,
        DateTime PushTime,
        DateTime? ResponseTime,
        string RequestContent,
        string? ResponseContent,
        string? ErrorMessage,
        int RetryCount)
    {
        public ExternalPushLog ToEntity(Guid auditUserId, string auditUsername)
        {
            return new ExternalPushLog
            {
                Id = Guid.NewGuid(),
                BusinessType = BusinessType,
                BusinessId = BusinessId,
                BusinessNoSnapshot = BusinessNoSnapshot,
                PlatformCode = PlatformCode,
                PushStatus = PushStatus,
                PushTime = PushTime,
                ResponseTime = ResponseTime,
                RequestContent = RequestContent,
                ResponseContent = ResponseContent,
                ErrorMessage = ErrorMessage,
                RetryCount = RetryCount,
                CreateBy = auditUserId,
                CreateName = auditUsername,
                UpdateTime = ResponseTime ?? PushTime,
                UpdateBy = auditUserId,
                UpdateName = auditUsername,
                Status = Status.Enable
            };
        }

        public void Validate(ExternalPushLog log, Guid auditUserId, string auditUsername)
        {
            if (log.BusinessType != BusinessType
                || log.BusinessId != BusinessId
                || log.BusinessNoSnapshot != BusinessNoSnapshot
                || log.PlatformCode != PlatformCode
                || log.PushStatus != PushStatus
                || log.PushTime != PushTime
                || log.ResponseTime != ResponseTime
                || log.RequestContent != RequestContent
                || log.ResponseContent != ResponseContent
                || log.ErrorMessage != ErrorMessage
                || log.RetryCount != RetryCount
                || log.CreateTime is null
                || log.CreateBy != auditUserId
                || log.CreateName != auditUsername
                || log.UpdateTime != (ResponseTime ?? PushTime)
                || log.UpdateBy != auditUserId
                || log.UpdateName != auditUsername
                || log.Status != Status.Enable)
            {
                throw new InvalidOperationException(
                    $"受管外部报送日志 {StableKey} 的来源、状态、脱敏报文、时间或审计指纹已漂移。");
            }
        }
    }
}
