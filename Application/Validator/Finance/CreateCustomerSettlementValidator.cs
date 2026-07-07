using Application.DTOs.Finance;
using FluentValidation;

namespace Application.Validator.Finance;

/// <summary>
/// 创建客户结款凭证请求校验器。
/// </summary>
public class CreateCustomerSettlementValidator : AbstractValidator<CreateCustomerSettlementDto>
{
    /// <summary>
    /// 配置凭证流水、备注和待结账单明细的基础输入约束。
    /// </summary>
    public CreateCustomerSettlementValidator(IValidator<CreateCustomerSettlementDetailDto> detailValidator)
    {
        RuleFor(x => x.SerialNo).MaximumLength(100);
        RuleFor(x => x.Remark).MaximumLength(500);
        RuleFor(x => x.Details).NotNull().NotEmpty().Must(x => x is not null && x.Count <= 100)
            .WithMessage("单次客户结款最多处理 100 张账单");
        RuleForEach(x => x.Details).SetValidator(detailValidator);
    }
}
