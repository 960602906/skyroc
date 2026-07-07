using Application.DTOs.Finance;
using FluentValidation;

namespace Application.Validator.Finance;

/// <summary>
/// 作废客户结款凭证请求校验器。
/// </summary>
public class VoidCustomerSettlementValidator : AbstractValidator<VoidCustomerSettlementDto>
{
    /// <summary>
    /// 配置作废原因必填和长度约束。
    /// </summary>
    public VoidCustomerSettlementValidator()
    {
        RuleFor(x => x.Remark).NotEmpty().MaximumLength(500);
    }
}
