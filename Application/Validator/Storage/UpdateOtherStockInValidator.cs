using Application.DTOs.Storage;
using FluentValidation;

namespace Application.Validator;

/// <summary>
/// 其他入库草稿编辑请求校验器。
/// </summary>
public class UpdateOtherStockInValidator : AbstractValidator<UpdateOtherStockInDto>
{
    /// <summary>
    /// 配置主键、仓库、入库时间、备注及商品行约束。
    /// </summary>
    public UpdateOtherStockInValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.WareId).NotEmpty();
        RuleFor(x => x.InTime).NotEmpty();
        RuleFor(x => x.Remark).MaximumLength(500);
        RuleFor(x => x.Details).NotEmpty();
        RuleForEach(x => x.Details).SetValidator(new UpdateStockInDetailValidator());
    }
}
