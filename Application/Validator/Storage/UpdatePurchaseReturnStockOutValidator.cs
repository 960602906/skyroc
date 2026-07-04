using Application.DTOs.Storage;
using FluentValidation;

namespace Application.Validator;

/// <summary>
/// 采购退货出库草稿编辑请求校验器。
/// </summary>
public class UpdatePurchaseReturnStockOutValidator : AbstractValidator<UpdatePurchaseReturnStockOutDto>
{
    /// <summary>
    /// 配置主键、仓库、供应商、出库时间、备注及商品批次行约束。
    /// </summary>
    public UpdatePurchaseReturnStockOutValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.WareId).NotEmpty();
        RuleFor(x => x.SupplierId).NotEmpty();
        RuleFor(x => x.OutTime).NotEmpty();
        RuleFor(x => x.Remark).MaximumLength(500);
        RuleFor(x => x.Details).NotEmpty();
        RuleForEach(x => x.Details).SetValidator(new UpdateStockOutDetailValidator());
    }
}
