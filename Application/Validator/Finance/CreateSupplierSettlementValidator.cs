using Application.DTOs.Finance;
using FluentValidation;

namespace Application.Validator.Finance;

/// <summary>
/// 创建供应商结算单请求校验器。
/// </summary>
public class CreateSupplierSettlementValidator : AbstractValidator<CreateSupplierSettlementDto>
{
    /// <summary>
    /// 配置结算单流水、备注和待结单据明细的基础输入约束。
    /// </summary>
    public CreateSupplierSettlementValidator(IValidator<CreateSupplierSettlementDetailDto> detailValidator)
    {
        RuleFor(x => x.SerialNo).MaximumLength(100);
        RuleFor(x => x.Remark).MaximumLength(500);
        RuleFor(x => x.Details).NotNull().NotEmpty().Must(x => x is not null && x.Count <= 100)
            .WithMessage("单次供应商结算最多处理 100 张待结单据");
        RuleForEach(x => x.Details).SetValidator(detailValidator);
    }
}
