using Application.DTOs.Files;
using AutoMapper;
using Domain.Entities.Files;

namespace Application.Mappers;

/// <summary>
/// 受保护文件元数据与安全响应模型之间的机械映射配置。
/// </summary>
public class FileMappingProfile : Profile
{
    /// <summary>
    /// 配置原始文件名、已验证类型和字节数的响应映射。
    /// </summary>
    public FileMappingProfile()
    {
        CreateMap<StoredFile, StoredFileDto>()
            .ForMember(destination => destination.FileName, options => options.MapFrom(source => source.OriginalFileName))
            .ForMember(destination => destination.Size, options => options.MapFrom(source => source.FileSize));
    }
}
