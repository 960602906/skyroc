using Application.DTOs.Storage;
using FluentValidation;

namespace Application.Validator;

/// <summary>
/// 出库商品行请求校验器，约束库存批次、商品单位、数量、价格和备注。
/// </summary>
public class CreateStockOutDetailValidator : AbstractValidator<CreateStockOutDetailDto>
{
    /// <summary>
    /// 配置出库商品行的批次、单位、数量、单价和备注约束。
    /// </summary>
    public CreateStockOutDetailValidator()
    {
        RuleFor(x => x.StockBatchId).NotEmpty();
        RuleFor(x => x.GoodsUnitId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0m);
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0m);
        RuleFor(x => x.Remark).MaximumLength(500);
    }
}
