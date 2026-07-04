using Application.DTOs.Storage;
using Domain.Entities.Purchases;
using FluentValidation;

namespace Application.Validator;

/// <summary>
/// 采购入库创建请求校验器。
/// </summary>
public class CreatePurchaseStockInValidator : AbstractValidator<CreatePurchaseStockInDto>
{
    /// <summary>
    /// 配置仓库、供应商、采购模式、入库时间及商品行约束。
    /// </summary>
    public CreatePurchaseStockInValidator()
    {
        RuleFor(x => x.WareId).NotEmpty();
        RuleFor(x => x.PurchasePattern).IsInEnum();
        RuleFor(x => x.SupplierId)
            .NotNull()
            .When(x => x.PurchasePattern == PurchasePattern.SupplierDirect)
            .WithMessage("供应商直供采购入库必须选择供应商");
        RuleFor(x => x.InTime).NotEmpty();
        RuleFor(x => x.Remark).MaximumLength(500);
        RuleFor(x => x.Details).NotEmpty();
        RuleForEach(x => x.Details).SetValidator(new CreateStockInDetailValidator());
    }
}
