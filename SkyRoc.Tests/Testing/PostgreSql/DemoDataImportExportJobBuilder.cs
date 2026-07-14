using Domain.Entities.ImportExport;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace SkyRoc.Tests.Testing.PostgreSql;

/// <summary>
///     以完整稳定任务编号补齐导入导出长期联调快照，并在复用前验证执行结果和审计指纹。
/// </summary>
internal sealed class DemoDataImportExportJobBuilder(
    ApplicationDbContext context,
    Guid auditUserId,
    string auditUsername)
{
    private const string JobArea = "IMPORT-EXPORT-JOB";
    private const string JobNoPrefix = $"{DemoDataStableKeyCatalog.ManagedPrefix}-{JobArea}-";

    /// <summary>
    ///     补齐三十条商品导入导出任务，均衡覆盖方向及处理中、成功和失败状态。
    /// </summary>
    public async Task<DemoDataImportExportJobGenerationResult> GenerateAsync(
        CancellationToken cancellationToken)
    {
        var seeds = Enumerable.Range(1, 30)
            .Select(CreateSeed)
            .ToArray();
        var expectedJobNos = seeds.Select(seed => seed.JobNo).ToHashSet(StringComparer.Ordinal);
        var candidates = await context.ImportExportJobs
            .Where(job => job.JobNo.StartsWith(JobNoPrefix))
            .OrderBy(job => job.JobNo)
            .ToListAsync(cancellationToken);

        EnsureOnlyExpectedStableKeys(candidates.Select(job => job.JobNo), expectedJobNos);
        EnsureNoDuplicateStableKeys(candidates.Select(job => job.JobNo));
        var jobsByNo = candidates.ToDictionary(job => job.JobNo, StringComparer.Ordinal);
        var createdJobs = 0;
        var reusedJobs = 0;

        foreach (var seed in seeds)
        {
            if (!jobsByNo.TryGetValue(seed.JobNo, out var job))
            {
                job = seed.ToEntity(auditUserId, auditUsername);
                await context.ImportExportJobs.AddAsync(job, cancellationToken);
                jobsByNo.Add(seed.JobNo, job);
                createdJobs++;
                continue;
            }

            seed.Validate(job, auditUserId, auditUsername);
            reusedJobs++;
        }

        await context.SaveChangesAsync(cancellationToken);
        return new DemoDataImportExportJobGenerationResult(createdJobs, reusedJobs);
    }

    private static ImportExportJobSeed CreateSeed(int sequence)
    {
        var direction = sequence % 2 == 1
            ? ImportExportDirection.Import
            : ImportExportDirection.Export;
        var jobStatus = ((sequence - 1) % 3) switch
        {
            0 => ImportExportJobStatus.Processing,
            1 => ImportExportJobStatus.Succeeded,
            _ => ImportExportJobStatus.Failed
        };
        var startedAt = new DateTime(2026, 6, 1, 1, 0, 0, DateTimeKind.Utc)
            .AddDays(sequence - 1)
            .AddMinutes(sequence);
        var totalRows = jobStatus == ImportExportJobStatus.Processing ? 0 : 24 + sequence;
        DateTime? finishedAt = jobStatus == ImportExportJobStatus.Processing
            ? null
            : startedAt.AddMinutes(2 + sequence % 5);

        return new ImportExportJobSeed(
            DemoDataStableKeyCatalog.Create(JobArea, sequence),
            direction,
            jobStatus,
            direction == ImportExportDirection.Import
                ? $"goods-import-batch-{sequence:D3}.csv"
                : $"goods-export-batch-{sequence:D3}.csv",
            totalRows,
            jobStatus == ImportExportJobStatus.Succeeded ? totalRows : 0,
            jobStatus == ImportExportJobStatus.Failed ? totalRows : 0,
            jobStatus == ImportExportJobStatus.Failed
                ? $"第 {sequence + 1} 行商品分类不存在；整文件校验失败且未写入任何商品。"
                : null,
            startedAt,
            finishedAt);
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
                $"检测到未知的受管导入导出任务稳定键：{string.Join("、", unknownKeys)}。");
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
                $"检测到重复的受管导入导出任务稳定键：{string.Join("、", duplicateKeys)}。");
        }
    }

    private sealed record ImportExportJobSeed(
        string JobNo,
        ImportExportDirection Direction,
        ImportExportJobStatus JobStatus,
        string SourceFileName,
        int TotalRows,
        int SuccessRows,
        int FailureRows,
        string? ErrorSummary,
        DateTime StartedAt,
        DateTime? FinishedAt)
    {
        public ImportExportJob ToEntity(Guid auditUserId, string auditUsername)
        {
            return new ImportExportJob
            {
                Id = Guid.NewGuid(),
                JobNo = JobNo,
                JobType = ImportExportJobType.Goods,
                JobDirection = Direction,
                JobStatus = JobStatus,
                SourceFileName = SourceFileName,
                TotalRows = TotalRows,
                SuccessRows = SuccessRows,
                FailureRows = FailureRows,
                ErrorSummary = ErrorSummary,
                JobStartedAt = StartedAt,
                JobFinishedAt = FinishedAt,
                CreateBy = auditUserId,
                CreateName = auditUsername,
                UpdateTime = FinishedAt ?? StartedAt,
                UpdateBy = auditUserId,
                UpdateName = auditUsername,
                Status = Status.Enable
            };
        }

        public void Validate(ImportExportJob job, Guid auditUserId, string auditUsername)
        {
            if (job.JobType != ImportExportJobType.Goods
                || job.JobDirection != Direction
                || job.JobStatus != JobStatus
                || job.SourceFileName != SourceFileName
                || job.TotalRows != TotalRows
                || job.SuccessRows != SuccessRows
                || job.FailureRows != FailureRows
                || job.ErrorSummary != ErrorSummary
                || job.JobStartedAt != StartedAt
                || job.JobFinishedAt != FinishedAt
                || job.CreateTime is null
                || job.CreateBy != auditUserId
                || job.CreateName != auditUsername
                || job.UpdateTime != (FinishedAt ?? StartedAt)
                || job.UpdateBy != auditUserId
                || job.UpdateName != auditUsername
                || job.Status != Status.Enable)
            {
                throw new InvalidOperationException(
                    $"受管导入导出任务 {JobNo} 的方向、状态、行数、时间或审计指纹已漂移。");
            }
        }
    }
}
