using Application.DTOs.Storage;
using FluentValidation;

namespace Application.Validator;

/// <summary>
/// 销售出库草稿编辑请求校验器。
/// </summary>
public class UpdateSaleStockOutValidator : AbstractValidator<UpdateSaleStockOutDto>
{
    /// <summary>
    /// 配置主键、仓库、客户、出库时间、备注及商品批次行约束。
    /// </summary>
    public UpdateSaleStockOutValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.WareId).NotEmpty();
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.OutTime).NotEmpty();
        RuleFor(x => x.Remark).MaximumLength(500);
        RuleFor(x => x.Details).NotEmpty();
        RuleForEach(x => x.Details).SetValidator(new UpdateStockOutDetailValidator());
    }
}
