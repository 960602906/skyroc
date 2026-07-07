namespace Domain.Entities.Traceability;

/// <summary>
/// 检测报告附件，保存报告文件或现场图片的访问地址和展示顺序。
/// </summary>
public class InspectionAttachment : BaseEntity
{
    /// <summary>
    /// 所属检测报告主键。
    /// </summary>
    public Guid InspectionReportId { get; set; }

    /// <summary>
    /// 附件类型：报告文件或现场图片。
    /// </summary>
    public InspectionAttachmentType AttachmentType { get; set; }

    /// <summary>
    /// 附件原始文件名，含扩展名。
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// 附件的可访问存储地址。
    /// </summary>
    public string FileUrl { get; set; } = string.Empty;

    /// <summary>
    /// 附件文件大小，单位为字节；未记录时为空。
    /// </summary>
    public long? FileSize { get; set; }

    /// <summary>
    /// 同一报告内附件的展示顺序，值越小越靠前。
    /// </summary>
    public int Sort { get; set; }

    /// <summary>
    /// 所属检测报告主单。
    /// </summary>
    public virtual InspectionReport InspectionReport { get; set; } = null!;
}
