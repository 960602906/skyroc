using Application.DTOs.Files;
using Application.Interfaces;
using Domain.Entities.Files;
using Domain.Entities.Goods;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace SkyRoc.Tests.Testing.PostgreSql;

/// <summary>
///     通过安全文件服务保存真实商品图片文件，并只为完整稳定键对应的受管商品建立图片关系。
/// </summary>
internal sealed class DemoDataFileBuilder(
    ApplicationDbContext context,
    IFileStorageService fileStorageService,
    Guid auditUserId,
    string auditUsername)
{
    private const string ContentType = "image/png";
    private const string FileArea = "STORED-FILE";
    private const string FileNamePrefix = $"{DemoDataStableKeyCatalog.ManagedPrefix}-{FileArea}-";
    private static readonly byte[] PngContent = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");

    /// <summary>
    ///     补齐三十个真实 PNG 文件及对应商品主图，并在复用前验证归属、内容和审计指纹。
    /// </summary>
    public async Task<DemoDataFileGenerationResult> GenerateAsync(CancellationToken cancellationToken)
    {
        var seeds = Enumerable.Range(1, 30)
            .Select(sequence => new FileSeed(
                $"{DemoDataStableKeyCatalog.Create(FileArea, sequence)}.png",
                DemoDataStableKeyCatalog.Create("GOODS", sequence)))
            .ToArray();
        var expectedFileNames = seeds.Select(seed => seed.FileName).ToHashSet(StringComparer.Ordinal);
        var goodsCodes = seeds.Select(seed => seed.GoodsCode).ToArray();
        var managedGoods = await context.Goods
            .Where(goods => goodsCodes.Contains(goods.Code))
            .ToDictionaryAsync(goods => goods.Code, StringComparer.Ordinal, cancellationToken);
        if (managedGoods.Count != seeds.Length)
            throw new InvalidOperationException("安全文件联调数据生成需要完整的三十条受管商品资料。");

        var storedFileCandidates = await context.StoredFiles
            .Where(file => file.OriginalFileName.StartsWith(FileNamePrefix))
            .OrderBy(file => file.OriginalFileName)
            .ToListAsync(cancellationToken);
        EnsureOnlyExpectedStableKeys(
            storedFileCandidates.Select(file => file.OriginalFileName),
            expectedFileNames,
            "安全文件");

        // 自动清理历史残留重复键：删除所有同名记录后本轮按单一稳定键重建。
        var duplicateFileNames = storedFileCandidates
            .GroupBy(file => file.OriginalFileName, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();
        if (duplicateFileNames.Length > 0)
        {
            var duplicateFiles = storedFileCandidates
                .Where(file => duplicateFileNames.Contains(file.OriginalFileName))
                .ToArray();
            context.StoredFiles.RemoveRange(duplicateFiles);
            await context.SaveChangesAsync(cancellationToken);
            storedFileCandidates = storedFileCandidates.Except(duplicateFiles).ToList();
        }

        var storedFiles = storedFileCandidates.ToDictionary(
            file => file.OriginalFileName,
            StringComparer.Ordinal);

        var createdStoredFiles = 0;
        var reusedStoredFiles = 0;
        foreach (var seed in seeds)
        {
            if (!storedFiles.TryGetValue(seed.FileName, out var storedFile))
            {
                await using var content = new MemoryStream(PngContent, writable: false);
                var created = await fileStorageService.UploadAsync(
                    new FileUploadRequest
                    {
                        FileName = seed.FileName,
                        ContentType = ContentType,
                        Length = PngContent.Length,
                        Content = content
                    },
                    cancellationToken);
                storedFile = await context.StoredFiles.SingleAsync(
                    file => file.Id == created.Id,
                    cancellationToken);
                storedFiles.Add(seed.FileName, storedFile);
                createdStoredFiles++;
            }
            else
            {
                storedFile = await EnsureStoredFileContentAsync(storedFile, seed.FileName, cancellationToken);
                storedFiles[seed.FileName] = storedFile;
                reusedStoredFiles++;
            }
        }

        var goodsImageCandidates = await context.GoodsImages
            .Where(image => image.FileName != null && image.FileName.StartsWith(FileNamePrefix))
            .OrderBy(image => image.FileName)
            .ToListAsync(cancellationToken);
        EnsureOnlyExpectedStableKeys(
            goodsImageCandidates.Select(image => image.FileName!),
            expectedFileNames,
            "商品图片");

        // 自动清理历史残留重复键：删除所有同名图片后本轮按单一稳定键重建。
        var duplicateImageFileNames = goodsImageCandidates
            .GroupBy(image => image.FileName!, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();
        if (duplicateImageFileNames.Length > 0)
        {
            var duplicateImages = goodsImageCandidates
                .Where(image => duplicateImageFileNames.Contains(image.FileName!))
                .ToArray();
            context.GoodsImages.RemoveRange(duplicateImages);
            await context.SaveChangesAsync(cancellationToken);
            goodsImageCandidates = goodsImageCandidates.Except(duplicateImages).ToList();
        }

        var goodsImages = goodsImageCandidates.ToDictionary(image => image.FileName!, StringComparer.Ordinal);

        var managedGoodsIds = managedGoods.Values.Select(goods => goods.Id).ToArray();
        var conflictingPrimaryImages = await context.GoodsImages
            .Where(image => managedGoodsIds.Contains(image.GoodsId)
                            && image.IsPrimary
                            && (image.FileName == null || !expectedFileNames.Contains(image.FileName)))
            .Select(image => image.GoodsId)
            .Distinct()
            .ToArrayAsync(cancellationToken);
        if (conflictingPrimaryImages.Length > 0)
            throw new InvalidOperationException("受管商品已存在无法确认归属的主图，安全文件生成已停止且不会覆盖既有图片。");

        var createdGoodsImages = 0;
        var reusedGoodsImages = 0;
        foreach (var seed in seeds)
        {
            var goods = managedGoods[seed.GoodsCode];
            var storedFile = storedFiles[seed.FileName];
            var expectedUrl = $"/api/files/{storedFile.Id}/download";
            if (!goodsImages.TryGetValue(seed.FileName, out var image))
            {
                image = new GoodsImage
                {
                    Id = Guid.NewGuid(),
                    GoodsId = goods.Id,
                    Url = expectedUrl,
                    FileName = seed.FileName,
                    Sort = 1,
                    IsPrimary = true,
                    CreateBy = auditUserId,
                    CreateName = auditUsername
                };
                await context.GoodsImages.AddAsync(image, cancellationToken);
                createdGoodsImages++;
                continue;
            }

            if (image.GoodsId != goods.Id
                || image.Sort != 1
                || !image.IsPrimary
                || image.CreateBy != auditUserId)
            {
                throw new InvalidOperationException($"受管商品图片 {seed.FileName} 的来源、展示属性或审计指纹已漂移。");
            }

            // 安全文件重传后主键变化，允许就地刷新下载 URL（仅当 URL 不匹配时更新，避免触发 UpdateTime）。
            if (image.Url != expectedUrl)
            {
                image.Url = expectedUrl;
            }
            reusedGoodsImages++;
        }

        await context.SaveChangesAsync(cancellationToken);
        return new DemoDataFileGenerationResult(
            createdStoredFiles,
            reusedStoredFiles,
            createdGoodsImages,
            reusedGoodsImages);
    }

    private async Task<StoredFile> EnsureStoredFileContentAsync(
        StoredFile storedFile,
        string fileName,
        CancellationToken cancellationToken)
    {
        if (storedFile.ContentType != ContentType
            || storedFile.FileSize != PngContent.Length
            || string.IsNullOrWhiteSpace(storedFile.StorageKey))
        {
            throw new InvalidOperationException($"受管安全文件 {storedFile.OriginalFileName} 的元数据或审计指纹已漂移。");
        }

        if (storedFile.CreateBy != auditUserId)
        {
            throw new InvalidOperationException($"受管安全文件 {storedFile.OriginalFileName} 的创建人已漂移（期望 {auditUserId}，实际 {storedFile.CreateBy}）。");
        }

        try
        {
            await using var download = await fileStorageService.DownloadAsync(storedFile.Id, cancellationToken);
            await using var copy = new MemoryStream();
            await download.Content.CopyToAsync(copy, cancellationToken);
            if (!copy.ToArray().AsSpan().SequenceEqual(PngContent))
                throw new InvalidOperationException($"受管安全文件 {storedFile.OriginalFileName} 的物理内容已漂移。");
            return storedFile;
        }
        catch (Application.Exceptions.NotFoundException)
        {
            // 元数据在库但对象存储丢失：删除后按稳定文件名重传。
            context.StoredFiles.Remove(storedFile);
            await context.SaveChangesAsync(cancellationToken);

            await using var content = new MemoryStream(PngContent, writable: false);
            var created = await fileStorageService.UploadAsync(
                new FileUploadRequest
                {
                    FileName = fileName,
                    ContentType = ContentType,
                    Length = PngContent.Length,
                    Content = content
                },
                cancellationToken);
            return await context.StoredFiles.SingleAsync(file => file.Id == created.Id, cancellationToken);
        }
    }

    private static void EnsureOnlyExpectedStableKeys(
        IEnumerable<string> actualKeys,
        IReadOnlySet<string> expectedKeys,
        string displayName)
    {
        var unknownKeys = actualKeys.Where(key => !expectedKeys.Contains(key)).Distinct().Order().ToArray();
        if (unknownKeys.Length > 0)
            throw new InvalidOperationException($"检测到未知的受管{displayName}稳定键：{string.Join("、", unknownKeys)}。");
    }

    private static void EnsureNoDuplicateStableKeys(IEnumerable<string> actualKeys, string displayName)
    {
        var duplicateKeys = actualKeys
            .GroupBy(key => key, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .Order()
            .ToArray();
        if (duplicateKeys.Length > 0)
            throw new InvalidOperationException($"检测到重复的受管{displayName}稳定键：{string.Join("、", duplicateKeys)}。");
    }

    private sealed record FileSeed(string FileName, string GoodsCode);
}
