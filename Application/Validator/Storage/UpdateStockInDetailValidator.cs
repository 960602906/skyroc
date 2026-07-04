using Application.DTOs.Storage;
using FluentValidation;

namespace Application.Validator;

/// <summary>
/// 入库商品行编辑校验器，复用创建约束并允许携带明细主键。
/// </summary>
public class UpdateStockInDetailValidator : AbstractValidator<UpdateStockInDetailDto>
{
    /// <summary>
    /// 配置编辑入库商品行的商品、单位、数量、单价、批次和备注约束。
    /// </summary>
    public UpdateStockInDetailValidator()
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
