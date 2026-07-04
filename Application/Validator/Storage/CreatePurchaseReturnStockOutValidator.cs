using Application.DTOs.Storage;
using FluentValidation;

namespace Application.Validator;

/// <summary>
/// 采购退货出库创建请求校验器。
/// </summary>
public class CreatePurchaseReturnStockOutValidator : AbstractValidator<CreatePurchaseReturnStockOutDto>
{
    /// <summary>
    /// 配置仓库、供应商、出库时间、备注及商品批次行约束。
    /// </summary>
    public CreatePurchaseReturnStockOutValidator()
    {
        RuleFor(x => x.WareId).NotEmpty();
        RuleFor(x => x.SupplierId).NotEmpty();
        RuleFor(x => x.OutTime).NotEmpty();
        RuleFor(x => x.Remark).MaximumLength(500);
        RuleFor(x => x.Details).NotEmpty();
        RuleForEach(x => x.Details).SetValidator(new CreateStockOutDetailValidator());
    }
}
