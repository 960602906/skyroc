using Application.DTOs.Files;
using Application.Mappers;
using AutoMapper;
using Domain.Entities.Files;
using Xunit;

namespace SkyRoc.Tests.Mapping;

/// <summary>
/// 验证受保护文件响应使用机械映射，并由文件主键计算受保护下载地址。
/// </summary>
public class FileMappingProfileTests
{
    [Fact]
    public void StoredFileMapping_MapsMetadataAndComputesDownloadUrl()
    {
        var configuration = new MapperConfiguration(config => config.AddProfile<FileMappingProfile>());
        configuration.AssertConfigurationIsValid();
        var mapper = configuration.CreateMapper();
        var id = Guid.NewGuid();

        var result = mapper.Map<StoredFileDto>(new StoredFile
        {
            Id = id,
            OriginalFileName = "report.pdf",
            ContentType = "application/pdf",
            FileSize = 128
        });

        Assert.Equal(id, result.Id);
        Assert.Equal("report.pdf", result.FileName);
        Assert.Equal("application/pdf", result.ContentType);
        Assert.Equal(128, result.Size);
        Assert.Equal($"/api/files/{id}/download", result.DownloadUrl);
        Assert.Null(typeof(StoredFileDto).GetProperty(nameof(StoredFileDto.DownloadUrl))!.SetMethod);
    }
}
