using Application.DTOs.Storage;
using FluentValidation;

namespace Application.Validator;

/// <summary>
/// 销售退货入库创建请求校验器。
/// </summary>
public class CreateSalesReturnStockInValidator : AbstractValidator<CreateSalesReturnStockInDto>
{
    /// <summary>
    /// 配置仓库、退货客户、入库时间、备注及商品行约束。
    /// </summary>
    public CreateSalesReturnStockInValidator()
    {
        RuleFor(x => x.WareId).NotEmpty();
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.InTime).NotEmpty();
        RuleFor(x => x.Remark).MaximumLength(500);
        RuleFor(x => x.Details).NotEmpty();
        RuleForEach(x => x.Details).SetValidator(new CreateStockInDetailValidator());
    }
}
