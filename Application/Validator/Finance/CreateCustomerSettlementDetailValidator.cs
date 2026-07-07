using Application.DTOs.Finance;
using FluentValidation;

namespace Application.Validator.Finance;

/// <summary>
/// 创建客户结款凭证明细请求校验器。
/// </summary>
public class CreateCustomerSettlementDetailValidator : AbstractValidator<CreateCustomerSettlementDetailDto>
{
    /// <summary>
    /// 配置账单主键、收款金额、优惠金额和单行备注约束。
    /// </summary>
    public CreateCustomerSettlementDetailValidator()
    {
        RuleFor(x => x.CustomerBillId).NotEmpty();
        RuleFor(x => x.PaymentAmount).GreaterThanOrEqualTo(0m);
        RuleFor(x => x.DiscountAmount).GreaterThanOrEqualTo(0m);
        RuleFor(x => x).Must(x => x.PaymentAmount > 0m || x.DiscountAmount > 0m)
            .WithMessage("每张账单的本次收款金额和优惠金额不能同时为 0");
        RuleFor(x => x.Remark).MaximumLength(500);
    }
}
