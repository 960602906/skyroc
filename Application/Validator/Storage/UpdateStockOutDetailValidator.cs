using Application.DTOs.Storage;
using FluentValidation;

namespace Application.Validator;

/// <summary>
/// 出库商品行编辑校验器，复用批次、单位、数量、价格和备注约束。
/// </summary>
public class UpdateStockOutDetailValidator : AbstractValidator<UpdateStockOutDetailDto>
{
    /// <summary>
    /// 配置编辑出库商品行的批次、单位、数量、单价和备注约束。
    /// </summary>
    public UpdateStockOutDetailValidator()
    {
        RuleFor(x => x.StockBatchId).NotEmpty();
        RuleFor(x => x.GoodsUnitId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0m);
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0m);
        RuleFor(x => x.Remark).MaximumLength(500);
    }
}
