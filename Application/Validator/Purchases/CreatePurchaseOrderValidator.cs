using Application.DTOs.Purchases;
using Domain.Entities.Purchases;
using FluentValidation;

namespace Application.Validator;

/// <summary>
/// 手工创建采购单请求校验器。
/// </summary>
public class CreatePurchaseOrderValidator : AbstractValidator<CreatePurchaseOrderDto>
{
    /// <summary>
    /// 配置采购模式、责任人、联系人、备注及商品行约束。
    /// </summary>
    public CreatePurchaseOrderValidator()
    {
        RuleFor(x => x.PurchasePattern).IsInEnum();
        RuleFor(x => x.SupplierId)
            .NotNull()
            .When(x => x.PurchasePattern == PurchasePattern.SupplierDirect)
            .WithMessage("供应商直供采购单必须选择供应商");
        RuleFor(x => x.SupplierContactName).MaximumLength(50);
        RuleFor(x => x.SupplierContactPhone).MaximumLength(20);
        RuleFor(x => x.Remark).MaximumLength(500);
        RuleFor(x => x.Details).NotEmpty();
        RuleForEach(x => x.Details).SetValidator(new CreatePurchaseOrderDetailValidator());
    }
}
