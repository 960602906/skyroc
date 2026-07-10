using Application.DTOs.Traceability;
using FluentValidation;

namespace Application.Validator.Traceability;

/// <summary>检测报告保存请求验证器，保证来源、数量、枚举值和附件元数据在进入业务服务前合法。</summary>
public class SaveInspectionReportValidator : AbstractValidator<SaveInspectionReportDto>
{
    /// <summary>初始化检测报告及其商品、附件的输入约束。</summary>
    public SaveInspectionReportValidator()
    {
        RuleFor(x => x.StockInOrderId).NotEmpty().WithMessage("请选择来源采购入库单");
        RuleFor(x => x.InspectionOrg).NotEmpty().MaximumLength(150);
        RuleFor(x => x.InspectTime).NotEqual(default(DateTime)).WithMessage("请填写检测完成时间");
        RuleFor(x => x.Conclusion).IsInEnum();
        RuleFor(x => x.Remark).MaximumLength(500);
        RuleFor(x => x.Goods).NotEmpty().WithMessage("至少选择一项送检商品");
        RuleForEach(x => x.Goods).SetValidator(new SaveInspectionReportGoodsValidator());
        RuleForEach(x => x.Attachments).SetValidator(new SaveInspectionAttachmentValidator());
    }
}
