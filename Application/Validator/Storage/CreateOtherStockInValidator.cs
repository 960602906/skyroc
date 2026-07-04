using Application.DTOs.Storage;
using FluentValidation;

namespace Application.Validator;

/// <summary>
/// 其他入库创建请求校验器。
/// </summary>
public class CreateOtherStockInValidator : AbstractValidator<CreateOtherStockInDto>
{
    /// <summary>
    /// 配置仓库、入库时间、备注及商品行约束。
    /// </summary>
    public CreateOtherStockInValidator()
    {
        RuleFor(x => x.WareId).NotEmpty();
        RuleFor(x => x.InTime).NotEmpty();
        RuleFor(x => x.Remark).MaximumLength(500);
        RuleFor(x => x.Details).NotEmpty();
        RuleForEach(x => x.Details).SetValidator(new CreateStockInDetailValidator());
    }
}
