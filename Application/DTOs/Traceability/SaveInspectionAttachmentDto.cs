using Domain.Entities.Traceability;
namespace Application.DTOs.Traceability;
/// <summary>创建或编辑报告时的附件元数据输入。</summary>
public class SaveInspectionAttachmentDto
{
    /// <summary>附件类型。</summary>
    public InspectionAttachmentType AttachmentType { get; set; }
    /// <summary>原始文件名。</summary>
    public string FileName { get; set; } = string.Empty;
    /// <summary>文件可访问地址。</summary>
    public string FileUrl { get; set; } = string.Empty;
    /// <summary>文件大小，单位为字节。</summary>
    public long? FileSize { get; set; }
    /// <summary>同一报告内展示顺序。</summary>
    public int Sort { get; set; }
}
