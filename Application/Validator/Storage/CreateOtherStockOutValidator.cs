using Application.DTOs.Storage;
using FluentValidation;

namespace Application.Validator;

/// <summary>
/// 其他出库创建请求校验器。
/// </summary>
public class CreateOtherStockOutValidator : AbstractValidator<CreateOtherStockOutDto>
{
    /// <summary>
    /// 配置仓库、出库时间、备注及商品批次行约束。
    /// </summary>
    public CreateOtherStockOutValidator()
    {
        RuleFor(x => x.WareId).NotEmpty();
        RuleFor(x => x.OutTime).NotEmpty();
        RuleFor(x => x.Remark).MaximumLength(500);
        RuleFor(x => x.Details).NotEmpty();
        RuleForEach(x => x.Details).SetValidator(new CreateStockOutDetailValidator());
    }
}
