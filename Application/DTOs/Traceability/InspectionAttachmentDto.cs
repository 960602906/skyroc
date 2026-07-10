using Domain.Entities.Traceability;
namespace Application.DTOs.Traceability;
/// <summary>检测报告附件响应，描述报告文件或现场图片的访问地址与展示顺序。</summary>
public class InspectionAttachmentDto : BaseDto
{
    /// <summary>附件类型。</summary>
    public InspectionAttachmentType AttachmentType { get; set; }
    /// <summary>原始文件名。</summary>
    public string FileName { get; set; } = string.Empty;
    /// <summary>文件可访问地址。</summary>
    public string FileUrl { get; set; } = string.Empty;
    /// <summary>文件大小，单位为字节。</summary>
    public long? FileSize { get; set; }
    /// <summary>展示顺序。</summary>
    public int Sort { get; set; }
}
