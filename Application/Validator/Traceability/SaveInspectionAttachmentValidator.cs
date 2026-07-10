using Application.DTOs.Traceability;
using FluentValidation;

namespace Application.Validator.Traceability;

/// <summary>检测报告附件输入验证器，限制文件元数据长度、非负大小和合法附件类型。</summary>
public class SaveInspectionAttachmentValidator : AbstractValidator<SaveInspectionAttachmentDto>
{
    /// <summary>初始化文件元数据约束。</summary>
    public SaveInspectionAttachmentValidator()
    {
        RuleFor(x => x.AttachmentType).IsInEnum();
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.FileUrl).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.FileSize).GreaterThanOrEqualTo(0).When(x => x.FileSize.HasValue);
        RuleFor(x => x.Sort).GreaterThanOrEqualTo(0);
    }
}
