using Application.DTOs.Delivery;
using Domain.Entities.Orders;
using FluentValidation;

namespace Application.Validator;

/// <summary>
/// 配送商品验收请求校验器。
/// </summary>
public class SignDeliveryCheckDetailValidator : AbstractValidator<SignDeliveryCheckDetailDto>
{
    /// <summary>
    /// 配置出库行主键、客户确认数量、验收结论和备注长度校验。
    /// </summary>
    public SignDeliveryCheckDetailValidator()
    {
        RuleFor(x => x.StockOutDetailId).NotEqual(Guid.Empty).WithMessage("销售出库明细 ID 不能为空");
        RuleFor(x => x.AcceptedBaseQuantity).GreaterThanOrEqualTo(0).WithMessage("客户确认数量不能小于 0");
        RuleFor(x => x.CheckStatus)
            .Must(status => status is OrderCustomerCheckStatus.Accepted or OrderCustomerCheckStatus.Rejected)
            .WithMessage("验收结论只能为通过或拒绝");
        RuleFor(x => x.Remark).MaximumLength(500).WithMessage("验收备注不能超过 500 个字符");
    }
}
