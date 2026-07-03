using Application.DTOs.Purchases;
using Domain.Entities.Purchases;
using FluentValidation;

namespace Application.Validator;

/// <summary>
/// 编辑采购单请求校验器。
/// </summary>
public class UpdatePurchaseOrderValidator : AbstractValidator<UpdatePurchaseOrderDto>
{
    /// <summary>
    /// 配置主键、采购模式、联系人、备注和完整商品行约束。
    /// </summary>
    public UpdatePurchaseOrderValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.PurchasePattern).IsInEnum();
        RuleFor(x => x.SupplierId)
            .NotNull()
            .When(x => x.PurchasePattern == PurchasePattern.SupplierDirect)
            .WithMessage("供应商直供采购单必须选择供应商");
        RuleFor(x => x.SupplierContactName).MaximumLength(50);
        RuleFor(x => x.SupplierContactPhone).MaximumLength(20);
        RuleFor(x => x.Remark).MaximumLength(500);
        RuleFor(x => x.Details).NotEmpty();
        RuleFor(x => x.Details)
            .Must(details => details.Where(x => x.Id.HasValue).Select(x => x.Id).Distinct().Count()
                             == details.Count(x => x.Id.HasValue))
            .WithMessage("采购单商品行主键不能重复");
        RuleForEach(x => x.Details).SetValidator(new UpdatePurchaseOrderDetailValidator());
    }
}
