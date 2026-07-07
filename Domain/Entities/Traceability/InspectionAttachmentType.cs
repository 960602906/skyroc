namespace Domain.Entities.Traceability;

/// <summary>
/// 检测报告附件类型，区分正式报告文件和现场佐证图片。
/// </summary>
public enum InspectionAttachmentType
{
    /// <summary>
    /// 报告文件：检测机构出具的正式报告文档或扫描件。
    /// </summary>
    Report = 1,

    /// <summary>
    /// 现场图片：抽样、检测过程或商品实拍的佐证图片。
    /// </summary>
    Image = 2
}
