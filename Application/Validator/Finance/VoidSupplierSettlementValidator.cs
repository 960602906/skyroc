using Application.DTOs.Finance;
using FluentValidation;

namespace Application.Validator.Finance;

/// <summary>
/// 作废供应商结算单请求校验器。
/// </summary>
public class VoidSupplierSettlementValidator : AbstractValidator<VoidSupplierSettlementDto>
{
    /// <summary>
    /// 配置作废原因必填和长度约束。
    /// </summary>
    public VoidSupplierSettlementValidator()
    {
        RuleFor(x => x.Remark).NotEmpty().MaximumLength(500);
    }
}
