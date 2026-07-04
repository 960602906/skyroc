using Application.DTOs.Storage;
using FluentValidation;

namespace Application.Validator;

/// <summary>
/// 库存盘点创建请求校验器，约束仓库、批次集合唯一性和盘点说明。
/// </summary>
public class CreateStocktakingValidator : AbstractValidator<CreateStocktakingDto>
{
    /// <summary>
    /// 配置盘点仓库、备注、至少一个批次以及批次不重复约束。
    /// </summary>
    public CreateStocktakingValidator()
    {
        RuleFor(x => x.WareId).NotEmpty();
        RuleFor(x => x.Remark).MaximumLength(500);
        RuleFor(x => x.Details).NotEmpty();
        RuleForEach(x => x.Details).SetValidator(new CreateStocktakingDetailValidator());
        RuleFor(x => x.Details)
            .Must(details => details is null
                             || details.Select(detail => detail.StockBatchId).Distinct().Count() == details.Count)
            .WithMessage("同一盘点单不能重复选择库存批次");
    }
}
