using Application.DTOs.Orders;
using FluentValidation;

namespace Application.Validator;

public class UpdateSaleOrderValidator : AbstractValidator<UpdateSaleOrderDto>
{
    public UpdateSaleOrderValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("id 必须填写");
        RuleFor(x => x.CustomerId).NotEmpty().WithMessage("客户不能为空");
        RuleFor(x => x.OrderDate).NotEmpty().WithMessage("下单时间不能为空");
        RuleFor(x => x.ReceiveDate)
            .GreaterThanOrEqualTo(x => x.OrderDate)
            .When(x => x.ReceiveDate.HasValue)
            .WithMessage("收货时间不能早于下单时间");
        RuleFor(x => x.ContactName).MaximumLength(50);
        RuleFor(x => x.ContactPhone).MaximumLength(20);
        RuleFor(x => x.DeliveryAddress).MaximumLength(300);
        RuleFor(x => x.Remark).MaximumLength(500);
        RuleFor(x => x.InnerRemark).MaximumLength(500);
        RuleFor(x => x.Details).NotEmpty().WithMessage("订单至少需要一条商品明细");
        RuleForEach(x => x.Details).SetValidator(new UpdateSaleOrderDetailValidator());
    }
}
