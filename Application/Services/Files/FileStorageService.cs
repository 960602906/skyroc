using Application.DTOs.Files;
using Application.Exceptions;
using Application.interfaces;
using AutoMapper;
using Domain.Entities.Files;
using Domain.Interfaces;
using Microsoft.Extensions.Options;
using Shared.Common;

namespace Application.Services;

/// <summary>
/// 将签名已验证的有限类型文件保存到经过静态目录隔离验证的非公开位置，并按创建人隔离下载访问。
/// </summary>
public class FileStorageService : IFileStorageService
{
    private const string PdfContentType = "application/pdf";
    private const string PngContentType = "image/png";
    private const string JpegContentType = "image/jpeg";
    private readonly IStoredFileRepository _storedFileRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;
    private readonly string _storageRoot;

    /// <summary>
    /// 初始化安全存储服务，并拒绝落在 Web 根目录内的文件存储配置。
    /// </summary>
    public FileStorageService(
        IStoredFileRepository storedFileRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IMapper mapper,
        IOptions<FileStorageOptions> options,
        IFileStoragePathProvider pathProvider)
    {
        _storedFileRepository = storedFileRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _mapper = mapper;
        var storageRoot = options.Value.StorageRoot;
        if (string.IsNullOrWhiteSpace(storageRoot)) throw new InvalidOperationException("必须配置文件存储目录");
        _storageRoot = Path.GetFullPath(storageRoot, pathProvider.ContentRootPath);
        if (!string.IsNullOrWhiteSpace(pathProvider.WebRootPath)
            && IsSameOrDescendant(Path.GetFullPath(pathProvider.WebRootPath), _storageRoot))
        {
            throw new InvalidOperationException("文件存储目录不能位于 Web 根目录内");
        }
    }

    /// <inheritdoc />
    public async Task<StoredFileDto> UploadAsync(FileUploadRequest upload, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(upload);
        if (upload.Content is null || !upload.Content.CanRead)
        {
            throw new BusinessException("上传文件内容不可读取");
        }
        if (upload.Length is <= 0 or > FileStorageOptions.MaxUploadSizeBytes)
        {
            throw new BusinessException("上传文件必须大于 0 且不超过 10 MiB");
        }

        var originalFileName = ValidateOriginalFileName(upload.FileName);

        await using var verifiedContent = await ReadAtMostMaxUploadSizeAsync(upload.Content, cancellationToken);
        if (verifiedContent.Length != upload.Length)
        {
            throw new BusinessException("上传文件大小与实际内容不一致");
        }

        var contentType = IdentifyContentType(verifiedContent.GetBuffer().AsSpan(0, checked((int)verifiedContent.Length)));
        var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
        if (!IsMatchingFileType(extension, NormalizeContentType(upload.ContentType), contentType))
        {
            throw new BusinessException("文件内容与文件名或 Content-Type 不匹配");
        }

        var userId = _currentUserService.GetUserId() ?? throw new BusinessException("无法识别当前操作人");
        var storageKey = $"{DateTime.UtcNow:yyyy/MM}/{Guid.NewGuid():N}{extension}";
        var physicalPath = GetPhysicalPath(storageKey);
        Directory.CreateDirectory(Path.GetDirectoryName(physicalPath)!);
        var createdFile = false;
        try
        {
            verifiedContent.Position = 0;
            await using (var destination = new FileStream(physicalPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true))
            {
                createdFile = true;
                await verifiedContent.CopyToAsync(destination, cancellationToken);
            }

            var storedFile = new StoredFile
            {
                Id = Guid.NewGuid(),
                StorageKey = storageKey,
                OriginalFileName = originalFileName,
                ContentType = contentType,
                FileSize = verifiedContent.Length,
                CreateBy = userId,
                CreateName = _currentUserService.GetUserName()
            };
            await _storedFileRepository.AddAsync(storedFile);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return _mapper.Map<StoredFileDto>(storedFile);
        }
        catch
        {
            if (createdFile) TryDelete(physicalPath);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<StoredFileContent> DownloadAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.GetUserId() ?? throw new BusinessException("无法识别当前操作人");
        var storedFile = await _storedFileRepository.GetByConditionAsync(file => file.Id == id && file.CreateBy == userId);
        if (storedFile is null) throw new NotFoundException("上传文件不存在");

        var physicalPath = GetPhysicalPath(storedFile.StorageKey);
        try
        {
            var content = new FileStream(physicalPath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
            return new StoredFileContent { Content = content, FileName = storedFile.OriginalFileName, ContentType = storedFile.ContentType };
        }
        catch (Exception exception) when (exception is FileNotFoundException or DirectoryNotFoundException)
        {
            throw new NotFoundException("上传文件不存在");
        }
    }

    private string GetPhysicalPath(string storageKey)
    {
        var physicalPath = Path.GetFullPath(Path.Combine(_storageRoot, storageKey.Replace('/', Path.DirectorySeparatorChar)));
        var rootPrefix = _storageRoot.EndsWith(Path.DirectorySeparatorChar) ? _storageRoot : _storageRoot + Path.DirectorySeparatorChar;
        if (!physicalPath.StartsWith(rootPrefix, StringComparison.Ordinal)) throw new BusinessException("文件存储路径无效");
        return physicalPath;
    }

    private static string IdentifyContentType(ReadOnlySpan<byte> content)
    {
        if (content.StartsWith("%PDF-"u8)) return PdfContentType;
        if (content.Length >= 8 && content[..8].SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })) return PngContentType;
        if (content.Length >= 3 && content[..3].SequenceEqual(new byte[] { 255, 216, 255 })) return JpegContentType;
        throw new BusinessException("文件内容不是允许的 PDF、PNG 或 JPEG 格式");
    }

    private static bool IsMatchingFileType(string extension, string declaredContentType, string detectedContentType) =>
        (extension, declaredContentType, detectedContentType) switch
        {
            (".pdf", PdfContentType, PdfContentType) => true,
            (".png", PngContentType, PngContentType) => true,
            (".jpg" or ".jpeg", JpegContentType, JpegContentType) => true,
            _ => false
        };

    private static async Task<MemoryStream> ReadAtMostMaxUploadSizeAsync(Stream content, CancellationToken cancellationToken)
    {
        var verifiedContent = new MemoryStream();
        var buffer = new byte[81920];
        while (true)
        {
            var read = await content.ReadAsync(buffer, cancellationToken);
            if (read == 0) break;
            if (verifiedContent.Length + read > FileStorageOptions.MaxUploadSizeBytes)
            {
                await verifiedContent.DisposeAsync();
                throw new BusinessException("上传文件实际大小超过 10 MiB");
            }
            await verifiedContent.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }
        return verifiedContent;
    }

    private static string ValidateOriginalFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName) || fileName.Length > 255)
        {
            throw new BusinessException("上传文件名无效或超过 255 个字符");
        }
        if (fileName.IndexOfAny(['/', '\\']) >= 0 || fileName.Any(char.IsControl))
        {
            throw new BusinessException("上传文件名不能包含路径或控制字符");
        }
        return fileName;
    }

    private static string NormalizeContentType(string contentType) =>
        string.IsNullOrWhiteSpace(contentType) ? string.Empty : contentType.Trim().Split(';', 2)[0].ToLowerInvariant();

    private static bool IsSameOrDescendant(string parentPath, string candidatePath)
    {
        var relativePath = Path.GetRelativePath(parentPath, candidatePath);
        if (Path.IsPathFullyQualified(relativePath)) return false;
        return relativePath is "." or "" || (!relativePath.Equals("..", StringComparison.Ordinal)
            && !relativePath.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal));
    }

    private static void TryDelete(string physicalPath)
    {
        try { File.Delete(physicalPath); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
