using Application.DTOs.Finance;
using FluentValidation;

namespace Application.Validator.Finance;

/// <summary>
/// 创建供应商结算单明细请求校验器。
/// </summary>
public class CreateSupplierSettlementDetailValidator : AbstractValidator<CreateSupplierSettlementDetailDto>
{
    /// <summary>
    /// 配置待结单据主键、付款金额、优惠金额和单行备注约束。
    /// </summary>
    public CreateSupplierSettlementDetailValidator()
    {
        RuleFor(x => x.SupplierBillId).NotEmpty();
        RuleFor(x => x.PaymentAmount).GreaterThanOrEqualTo(0m);
        RuleFor(x => x.DiscountAmount).GreaterThanOrEqualTo(0m);
        RuleFor(x => x).Must(x => x.PaymentAmount > 0m || x.DiscountAmount > 0m)
            .WithMessage("每张待结单据的本次付款金额和优惠金额不能同时为 0");
        RuleFor(x => x.Remark).MaximumLength(500);
    }
}
