using Application.DTOs.Traceability;
using FluentValidation;
using Shared.Constants;

namespace Application.Validator.Traceability;

/// <summary>检测报告商品输入验证器，保证送检数量按全局数量精度舍入后仍为正数。</summary>
public class SaveInspectionReportGoodsValidator : AbstractValidator<SaveInspectionReportGoodsDto>
{
    /// <summary>初始化来源明细、数量、结论和说明约束。</summary>
    public SaveInspectionReportGoodsValidator()
    {
        RuleFor(x => x.StockInDetailId).NotEmpty();
        RuleFor(x => x.SampleQuantity).Must(x => NumericPrecision.RoundQuantity(x) > 0m).WithMessage("送检数量必须大于零");
        RuleFor(x => x.Conclusion).IsInEnum();
        RuleFor(x => x.Remark).MaximumLength(500);
    }
}
