using Application.DTOs.Storage;
using FluentValidation;

namespace Application.Validator;

/// <summary>
/// 盘点批次实盘数量校验器，约束批次标识、非负数量和差异说明长度。
/// </summary>
public class CreateStocktakingDetailValidator : AbstractValidator<CreateStocktakingDetailDto>
{
    /// <summary>
    /// 配置盘点批次、实盘基础单位数量和备注约束。
    /// </summary>
    public CreateStocktakingDetailValidator()
    {
        RuleFor(x => x.StockBatchId).NotEmpty();
        RuleFor(x => x.ActualQuantity).GreaterThanOrEqualTo(0m);
        RuleFor(x => x.Remark).MaximumLength(500);
    }
}
