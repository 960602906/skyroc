using System.Globalization;
using System.Text;
using Application.DTOs.ImportExport;
using Application.Exceptions;
using Application.Interfaces;
using Domain.Entities.Goods;
using Domain.Entities.ImportExport;
using Domain.Interfaces;
using Shared.Constants;

namespace Application.Services;

/// <summary>统一导入导出任务应用服务，以 UTF-8 CSV 提供商品模板、整文件校验导入和当前数据导出。</summary>
public class ImportExportJobService(
    IImportExportJobRepository jobRepository,
    IGoodsRepository goodsRepository,
    IGoodsTypeRepository goodsTypeRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    IDocumentNoGenerator documentNoGenerator) : IImportExportJobService
{
    private static readonly string[] GoodsHeaders =
        ["Name", "Code", "GoodsTypeId", "Spec", "Brand", "Origin", "TaxRate", "IsOnSale", "Remark"];

    /// <inheritdoc />
    public Task<ImportExportFileDto> DownloadTemplateAsync(ImportExportJobType jobType)
    {
        EnsureSupported(jobType);
        var rows = new[]
        {
            GoodsHeaders,
            ["示例商品", "EXAMPLE_GOODS", "请填写商品分类 UUID", "规格", "品牌", "产地", "6.0000", "true", "可删除本示例行"]
        };
        return Task.FromResult(CreateFile("goods-import-template.csv", rows, new ImportExportJobDto()));
    }

    /// <inheritdoc />
    public async Task<ImportExportJobDto> ImportAsync(ImportExportJobType jobType, string fileName, Stream content)
    {
        EnsureSupported(jobType);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentNullException.ThrowIfNull(content);
        var job = await CreateJobAsync(jobType, ImportExportDirection.Import, fileName);
        try
        {
            var rows = await ReadCsvAsync(content);
            job.TotalRows = rows.Skip(1).Count(row => !row.All(string.IsNullOrWhiteSpace));
            var records = ParseGoodsRows(rows);
            job.TotalRows = records.Count;
            var errors = await ValidateGoodsAsync(records);
            if (errors.Count > 0)
            {
                CompleteFailure(job, errors, records.Count);
                await SaveJobAsync(job);
                return ToDto(job);
            }

            await unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                await goodsRepository.AddRangeAsync(records.Select(record => new Goods
                {
                    Id = Guid.NewGuid(),
                    Name = record.Name,
                    Code = record.Code,
                    GoodsTypeId = record.GoodsTypeId,
                    Spec = record.Spec,
                    Brand = record.Brand,
                    Origin = record.Origin,
                    TaxRate = record.TaxRate,
                    IsOnSale = record.IsOnSale,
                    Remark = record.Remark,
                    CreateBy = job.CreateBy,
                    CreateName = job.CreateName
                }));
                await unitOfWork.SaveChangesAsync();
            });
            job.SuccessRows = records.Count;
            job.JobStatus = ImportExportJobStatus.Succeeded;
            job.JobFinishedAt = DateTime.UtcNow;
        }
        catch (Exception exception) when (exception is not BusinessException)
        {
            unitOfWork.ClearChangeTracking();
            CompleteFailure(job, [exception is FormatException ? exception.Message : "导入文件处理失败"], job.TotalRows);
        }

        await SaveJobAsync(job);
        return ToDto(job);
    }

    /// <inheritdoc />
    public async Task<ImportExportFileDto> ExportAsync(ImportExportJobType jobType)
    {
        EnsureSupported(jobType);
        var job = await CreateJobAsync(jobType, ImportExportDirection.Export, "goods-export.csv");
        try
        {
            var goods = (await goodsRepository.GetAllAsync()).OrderBy(x => x.Code, StringComparer.Ordinal).ToList();
            var rows = new List<string[]> { GoodsHeaders };
            rows.AddRange(goods.Select(x => new[]
            {
                x.Name, x.Code, x.GoodsTypeId.ToString(), x.Spec ?? string.Empty, x.Brand ?? string.Empty,
                x.Origin ?? string.Empty, x.TaxRate?.ToString("0.0000", CultureInfo.InvariantCulture) ?? string.Empty,
                x.IsOnSale ? "true" : "false", x.Remark ?? string.Empty
            }));
            job.TotalRows = goods.Count;
            job.SuccessRows = goods.Count;
            job.JobStatus = ImportExportJobStatus.Succeeded;
            job.JobFinishedAt = DateTime.UtcNow;
            await SaveJobAsync(job);
            return CreateFile(job.SourceFileName, rows, ToDto(job));
        }
        catch
        {
            CompleteFailure(job, ["导出文件生成失败"], job.TotalRows);
            await SaveJobAsync(job);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ImportExportJobDto> GetByIdAsync(Guid id)
    {
        var userId = GetCurrentUserId();
        var job = await jobRepository.GetByIdAsync(id);
        if (job is null || job.CreateBy != userId)
        {
            throw new NotFoundException("导入导出任务不存在");
        }
        return ToDto(job);
    }

    private async Task<ImportExportJob> CreateJobAsync(ImportExportJobType jobType, ImportExportDirection direction, string fileName)
    {
        var userId = GetCurrentUserId();
        var job = new ImportExportJob
        {
            Id = Guid.NewGuid(),
            JobNo = await documentNoGenerator.NextAsync(
                DocumentNoKind.ImportExportJob,
                no => jobRepository.ExistsAsync(x => x.JobNo == no)),
            JobType = jobType,
            JobDirection = direction,
            JobStatus = ImportExportJobStatus.Processing,
            SourceFileName = Path.GetFileName(fileName),
            JobStartedAt = DateTime.UtcNow,
            CreateBy = userId,
            CreateName = currentUserService.GetUserName()
        };
        await jobRepository.AddAsync(job);
        await unitOfWork.SaveChangesAsync();
        return job;
    }

    private async Task SaveJobAsync(ImportExportJob job)
    {
        job.UpdateBy = job.CreateBy;
        job.UpdateName = job.CreateName;
        await jobRepository.UpdateAsync(job);
        await unitOfWork.SaveChangesAsync();
    }

    private static List<GoodsImportRow> ParseGoodsRows(IReadOnlyList<string[]> rows)
    {
        if (rows.Count == 0 || !rows[0].SequenceEqual(GoodsHeaders, StringComparer.OrdinalIgnoreCase))
        {
            throw new FormatException("CSV 表头必须为 Name,Code,GoodsTypeId,Spec,Brand,Origin,TaxRate,IsOnSale,Remark");
        }
        var records = new List<GoodsImportRow>();
        for (var index = 1; index < rows.Count; index++)
        {
            var row = rows[index];
            if (row.All(string.IsNullOrWhiteSpace)) continue;
            if (row.Length != GoodsHeaders.Length) throw new FormatException($"第 {index + 1} 行列数不正确");
            if (!Guid.TryParse(row[2], out var goodsTypeId)) throw new FormatException($"第 {index + 1} 行商品分类 ID 无效");
            if (!decimal.TryParse(row[6], NumberStyles.Number, CultureInfo.InvariantCulture, out var taxRate) || taxRate is < 0 or > 100 || NumericPrecision.RoundMoney(taxRate) != taxRate)
                throw new FormatException($"第 {index + 1} 行税率必须是 0 至 100 之间的百分比");
            var isOnSale = row[7] switch
            {
                "true" => true,
                "false" => false,
                _ => throw new FormatException($"第 {index + 1} 行上架状态必须是 true 或 false")
            };
            records.Add(new GoodsImportRow
            {
                RowNumber = index + 1,
                Name = row[0].Trim(),
                Code = row[1].Trim(),
                GoodsTypeId = goodsTypeId,
                Spec = EmptyToNull(row[3]),
                Brand = EmptyToNull(row[4]),
                Origin = EmptyToNull(row[5]),
                TaxRate = taxRate,
                IsOnSale = isOnSale,
                Remark = EmptyToNull(row[8])
            });
        }
        if (records.Count == 0) throw new FormatException("CSV 至少需要一条商品数据");
        return records;
    }

    private async Task<List<string>> ValidateGoodsAsync(IEnumerable<GoodsImportRow> records)
    {
        var errors = new List<string>();
        var recordList = records.ToList();
        var existingGoodsTypeIds = (await goodsTypeRepository.GetByIdsAsync(recordList.Select(x => x.GoodsTypeId)))
            .Select(x => x.Id)
            .ToHashSet();
        var existingNames = (await goodsRepository.GetExistingNamesAsync(recordList.Select(x => x.Name)))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var existingCodes = (await goodsRepository.GetExistingCodesAsync(recordList.Select(x => x.Code)))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var row in recordList)
        {
            if (string.IsNullOrWhiteSpace(row.Name) || row.Name.Length > 150) errors.Add($"第 {row.RowNumber} 行商品名称不能为空且不得超过 150 个字符");
            if (string.IsNullOrWhiteSpace(row.Code) || row.Code.Length > 50) errors.Add($"第 {row.RowNumber} 行商品编码不能为空且不得超过 50 个字符");
            if (row.Spec?.Length > 100) errors.Add($"第 {row.RowNumber} 行商品规格不得超过 100 个字符");
            if (row.Brand?.Length > 100) errors.Add($"第 {row.RowNumber} 行商品品牌不得超过 100 个字符");
            if (row.Origin?.Length > 100) errors.Add($"第 {row.RowNumber} 行商品产地不得超过 100 个字符");
            if (row.Remark?.Length > 500) errors.Add($"第 {row.RowNumber} 行备注不得超过 500 个字符");
            if (!existingGoodsTypeIds.Contains(row.GoodsTypeId)) errors.Add($"第 {row.RowNumber} 行商品分类不存在");
            if (existingNames.Contains(row.Name)) errors.Add($"第 {row.RowNumber} 行商品名称已存在");
            if (existingCodes.Contains(row.Code)) errors.Add($"第 {row.RowNumber} 行商品编码已存在");
        }
        foreach (var group in recordList.GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase).Where(x => x.Count() > 1))
        {
            foreach (var row in group) errors.Add($"第 {row.RowNumber} 行商品名称重复：{row.Name}");
        }
        foreach (var group in recordList.GroupBy(x => x.Code, StringComparer.OrdinalIgnoreCase).Where(x => x.Count() > 1))
        {
            foreach (var row in group) errors.Add($"第 {row.RowNumber} 行商品编码重复：{row.Code}");
        }
        return errors;
    }

    private static async Task<List<string[]>> ReadCsvAsync(Stream content)
    {
        using var reader = new StreamReader(content, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var text = await reader.ReadToEndAsync();
        if (text.Length > 2 * 1024 * 1024) throw new FormatException("CSV 内容不能超过 2 MiB");
        var rows = new List<string[]>();
        var row = new List<string>();
        var field = new StringBuilder();
        var quoted = false;
        var closedQuote = false;
        for (var index = 0; index < text.Length; index++)
        {
            var character = text[index];
            if (character == '"')
            {
                if (quoted && index + 1 < text.Length && text[index + 1] == '"') { field.Append('"'); index++; }
                else if (quoted) { quoted = false; closedQuote = true; }
                else if (field.Length == 0 && !closedQuote) quoted = true;
                else throw new FormatException("CSV 引号只能出现在字段开头");
            }
            else if (character == ',' && !quoted) { row.Add(field.ToString()); field.Clear(); closedQuote = false; }
            else if ((character == '\n' || character == '\r') && !quoted)
            {
                if (character == '\r' && index + 1 < text.Length && text[index + 1] == '\n') index++;
                row.Add(field.ToString()); field.Clear(); rows.Add(row.ToArray()); row = []; closedQuote = false;
            }
            else
            {
                if (closedQuote) throw new FormatException("CSV 关闭引号后只能跟随分隔符或换行");
                field.Append(character);
            }
        }
        if (quoted) throw new FormatException("CSV 包含未闭合的引号");
        if (field.Length > 0 || row.Count > 0) { row.Add(field.ToString()); rows.Add(row.ToArray()); }
        return rows;
    }

    private static ImportExportFileDto CreateFile(string fileName, IEnumerable<string[]> rows, ImportExportJobDto job)
    {
        var content = string.Join("\r\n", rows.Select(row => string.Join(',', row.Select(EscapeCsv)))) + "\r\n";
        return new ImportExportFileDto { FileName = fileName, Content = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true).GetBytes(content), Job = job };
    }

    private static string EscapeCsv(string value)
    {
        var trimmed = value.TrimStart();
        if (trimmed.Length > 0 && "=+-@".Contains(trimmed[0])) value = "'" + value;
        return value.IndexOfAny([',', '"', '\r', '\n']) >= 0 ? $"\"{value.Replace("\"", "\"\"")}\"" : value;
    }
    private static string? EmptyToNull(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static void EnsureSupported(ImportExportJobType jobType) { if (jobType != ImportExportJobType.Goods) throw new BusinessException("不支持的导入导出任务类型"); }
    private Guid GetCurrentUserId() => currentUserService.GetUserId() ?? throw new BusinessException("无法识别当前操作人");
    private static void CompleteFailure(ImportExportJob job, IReadOnlyCollection<string> errors, int totalRows)
    {
        var summary = string.Join("；", errors.Take(20));
        job.JobStatus = ImportExportJobStatus.Failed;
        job.SuccessRows = 0;
        job.FailureRows = totalRows;
        job.ErrorSummary = summary.Length <= 4000 ? summary : summary[..3997] + "...";
        job.JobFinishedAt = DateTime.UtcNow;
    }
    private static ImportExportJobDto ToDto(ImportExportJob job) => new()
    {
        Id = job.Id,
        JobNo = job.JobNo,
        JobType = job.JobType,
        Direction = job.JobDirection,
        JobStatus = job.JobStatus,
        FileName = job.SourceFileName,
        TotalRows = job.TotalRows,
        SuccessRows = job.SuccessRows,
        FailureRows = job.FailureRows,
        ErrorSummary = job.ErrorSummary,
        StartedTime = job.JobStartedAt,
        FinishedTime = job.JobFinishedAt
    };
    private sealed class GoodsImportRow
    {
        public int RowNumber { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Code { get; init; } = string.Empty;
        public Guid GoodsTypeId { get; init; }
        public string? Spec { get; init; }
        public string? Brand { get; init; }
        public string? Origin { get; init; }
        public decimal TaxRate { get; init; }
        public bool IsOnSale { get; init; }
        public string? Remark { get; init; }
    }
}
