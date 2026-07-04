using Application.DTOs.Storage;
using FluentValidation;

namespace Application.Validator;

/// <summary>
/// 入库商品行请求校验器，约束商品、单位、数量、价格和批次号。
/// </summary>
public class CreateStockInDetailValidator : AbstractValidator<CreateStockInDetailDto>
{
    /// <summary>
    /// 配置入库商品行的商品、单位、数量、单价、批次和备注约束。
    /// </summary>
    public CreateStockInDetailValidator()
    {
        RuleFor(x => x.GoodsId).NotEmpty();
        RuleFor(x => x.GoodsUnitId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0m);
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0m);
        RuleFor(x => x.BatchNo).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Remark).MaximumLength(500);
        RuleFor(x => x.ExpireDate)
            .GreaterThanOrEqualTo(x => x.ProductDate!.Value)
            .When(x => x.ProductDate.HasValue && x.ExpireDate.HasValue)
            .WithMessage("到期日期不能早于生产日期");
    }
}
