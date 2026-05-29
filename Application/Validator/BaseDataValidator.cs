using Application.DTOs;
using Application.DTOs.Customers;
using Application.DTOs.Goods;
using Application.DTOs.Pricing;
using Application.DTOs.Purchases;
using Application.DTOs.Storage;
using FluentValidation;

namespace Application.Validator;

/// <summary>
///     带名称和编码的基础资料校验基类。
/// </summary>
public abstract class NamedCodeValidator<T> : AbstractValidator<T>
    where T : INamedCodeInput
{
    protected NamedCodeValidator(string displayName)
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage($"{displayName}名称不能为空")
            .MaximumLength(150).WithMessage($"{displayName}名称不能超过150个字符");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage($"{displayName}编码不能为空")
            .MaximumLength(50).WithMessage($"{displayName}编码不能超过50个字符")
            .Matches(@"^[A-Za-z0-9_\-]+$").WithMessage($"{displayName}编码只能包含字母、数字、下划线和中横线");
    }

    protected void RuleForId()
    {
        RuleFor(x => ((IUpdateInput)x).Id)
            .NotEmpty().WithMessage("id 必须填写");
    }
}

public class CreateGoodsTypeValidator : NamedCodeValidator<CreateGoodsTypeDto>
{
    public CreateGoodsTypeValidator() : base("商品分类")
    {
        RuleFor(x => x.DefaultTaxRate)
            .InclusiveBetween(0m, 1m)
            .When(x => x.DefaultTaxRate.HasValue)
            .WithMessage("默认税率必须在 0 到 1 之间");
    }
}

public class UpdateGoodsTypeValidator : NamedCodeValidator<UpdateGoodsTypeDto>
{
    public UpdateGoodsTypeValidator() : base("商品分类")
    {
        RuleForId();
        RuleFor(x => x.DefaultTaxRate)
            .InclusiveBetween(0m, 1m)
            .When(x => x.DefaultTaxRate.HasValue)
            .WithMessage("默认税率必须在 0 到 1 之间");
    }
}

public class CreateGoodsValidator : NamedCodeValidator<CreateGoodsDto>
{
    public CreateGoodsValidator() : base("商品")
    {
        RuleFor(x => x.GoodsTypeId).NotEmpty().WithMessage("商品分类不能为空");
        RuleFor(x => x.TaxRate)
            .InclusiveBetween(0m, 1m)
            .When(x => x.TaxRate.HasValue)
            .WithMessage("商品税率必须在 0 到 1 之间");
    }
}

public class UpdateGoodsValidator : NamedCodeValidator<UpdateGoodsDto>
{
    public UpdateGoodsValidator() : base("商品")
    {
        RuleForId();
        RuleFor(x => x.GoodsTypeId).NotEmpty().WithMessage("商品分类不能为空");
        RuleFor(x => x.TaxRate)
            .InclusiveBetween(0m, 1m)
            .When(x => x.TaxRate.HasValue)
            .WithMessage("商品税率必须在 0 到 1 之间");
    }
}

public class CreateGoodsUnitValidator : AbstractValidator<CreateGoodsUnitDto>
{
    public CreateGoodsUnitValidator()
    {
        RuleFor(x => x.GoodsId).NotEmpty().WithMessage("商品不能为空");
        RuleFor(x => x.Name).NotEmpty().WithMessage("单位名称不能为空").MaximumLength(50);
        RuleFor(x => x.ConversionRate).GreaterThan(0).WithMessage("换算比例必须大于0");
    }
}

public class UpdateGoodsUnitValidator : AbstractValidator<UpdateGoodsUnitDto>
{
    public UpdateGoodsUnitValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("id 必须填写");
        RuleFor(x => x.GoodsId).NotEmpty().WithMessage("商品不能为空");
        RuleFor(x => x.Name).NotEmpty().WithMessage("单位名称不能为空").MaximumLength(50);
        RuleFor(x => x.ConversionRate).GreaterThan(0).WithMessage("换算比例必须大于0");
    }
}

public class CreateCompanyValidator : NamedCodeValidator<CreateCompanyDto>
{
    public CreateCompanyValidator() : base("公司")
    {
    }
}

public class UpdateCompanyValidator : NamedCodeValidator<UpdateCompanyDto>
{
    public UpdateCompanyValidator() : base("公司")
    {
        RuleForId();
    }
}

public class CreateCustomerValidator : NamedCodeValidator<CreateCustomerDto>
{
    public CreateCustomerValidator() : base("客户")
    {
        RuleFor(x => x.TaxpayerIdentificationNumber).MaximumLength(32);
        RuleFor(x => x.UnifiedSocialCreditCode).MaximumLength(32);
    }
}

public class UpdateCustomerValidator : NamedCodeValidator<UpdateCustomerDto>
{
    public UpdateCustomerValidator() : base("客户")
    {
        RuleForId();
        RuleFor(x => x.TaxpayerIdentificationNumber).MaximumLength(32);
        RuleFor(x => x.UnifiedSocialCreditCode).MaximumLength(32);
    }
}

public class CreateCustomerTagValidator : NamedCodeValidator<CreateCustomerTagDto>
{
    public CreateCustomerTagValidator() : base("客户标签")
    {
    }
}

public class UpdateCustomerTagValidator : NamedCodeValidator<UpdateCustomerTagDto>
{
    public UpdateCustomerTagValidator() : base("客户标签")
    {
        RuleForId();
    }
}

public class CreateCustomerSubAccountValidator : AbstractValidator<CreateCustomerSubAccountDto>
{
    public CreateCustomerSubAccountValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty().WithMessage("所属公司不能为空");
        RuleFor(x => x.Username).NotEmpty().WithMessage("登录账号不能为空").MaximumLength(50);
        RuleFor(x => x.NickName).NotEmpty().WithMessage("昵称不能为空").MaximumLength(50);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email)).WithMessage("邮箱格式不正确");
    }
}

public class UpdateCustomerSubAccountValidator : AbstractValidator<UpdateCustomerSubAccountDto>
{
    public UpdateCustomerSubAccountValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("id 必须填写");
        RuleFor(x => x.CompanyId).NotEmpty().WithMessage("所属公司不能为空");
        RuleFor(x => x.Username).NotEmpty().WithMessage("登录账号不能为空").MaximumLength(50);
        RuleFor(x => x.NickName).NotEmpty().WithMessage("昵称不能为空").MaximumLength(50);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email)).WithMessage("邮箱格式不正确");
    }
}

public class CreateSupplierValidator : NamedCodeValidator<CreateSupplierDto>
{
    public CreateSupplierValidator() : base("供应商")
    {
    }
}

public class UpdateSupplierValidator : NamedCodeValidator<UpdateSupplierDto>
{
    public UpdateSupplierValidator() : base("供应商")
    {
        RuleForId();
    }
}

public class CreatePurchaserValidator : NamedCodeValidator<CreatePurchaserDto>
{
    public CreatePurchaserValidator() : base("采购员")
    {
    }
}

public class UpdatePurchaserValidator : NamedCodeValidator<UpdatePurchaserDto>
{
    public UpdatePurchaserValidator() : base("采购员")
    {
        RuleForId();
    }
}

public class CreateWareValidator : NamedCodeValidator<CreateWareDto>
{
    public CreateWareValidator() : base("仓库")
    {
    }
}

public class UpdateWareValidator : NamedCodeValidator<UpdateWareDto>
{
    public UpdateWareValidator() : base("仓库")
    {
        RuleForId();
    }
}

public class CreateQuotationValidator : NamedCodeValidator<CreateQuotationDto>
{
    public CreateQuotationValidator() : base("报价单")
    {
        RuleFor(x => x.EffectiveEnd)
            .GreaterThanOrEqualTo(x => x.EffectiveStart)
            .When(x => x.EffectiveStart.HasValue && x.EffectiveEnd.HasValue)
            .WithMessage("生效结束时间不能早于开始时间");
    }
}

public class UpdateQuotationValidator : NamedCodeValidator<UpdateQuotationDto>
{
    public UpdateQuotationValidator() : base("报价单")
    {
        RuleForId();
        RuleFor(x => x.EffectiveEnd)
            .GreaterThanOrEqualTo(x => x.EffectiveStart)
            .When(x => x.EffectiveStart.HasValue && x.EffectiveEnd.HasValue)
            .WithMessage("生效结束时间不能早于开始时间");
    }
}

public class CreateQuotationGoodsValidator : AbstractValidator<CreateQuotationGoodsDto>
{
    public CreateQuotationGoodsValidator()
    {
        RuleFor(x => x.QuotationId).NotEmpty().WithMessage("报价单不能为空");
        RuleFor(x => x.GoodsId).NotEmpty().WithMessage("商品不能为空");
        RuleFor(x => x.GoodsUnitId).NotEmpty().WithMessage("报价单位不能为空");
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0).WithMessage("销售单价不能小于0");
        RuleFor(x => x.MinOrderQuantity).GreaterThanOrEqualTo(0).When(x => x.MinOrderQuantity.HasValue);
    }
}

public class UpdateQuotationGoodsValidator : AbstractValidator<UpdateQuotationGoodsDto>
{
    public UpdateQuotationGoodsValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("id 必须填写");
        RuleFor(x => x.QuotationId).NotEmpty().WithMessage("报价单不能为空");
        RuleFor(x => x.GoodsId).NotEmpty().WithMessage("商品不能为空");
        RuleFor(x => x.GoodsUnitId).NotEmpty().WithMessage("报价单位不能为空");
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0).WithMessage("销售单价不能小于0");
        RuleFor(x => x.MinOrderQuantity).GreaterThanOrEqualTo(0).When(x => x.MinOrderQuantity.HasValue);
    }
}

public class CreateCustomerProtocolValidator : NamedCodeValidator<CreateCustomerProtocolDto>
{
    public CreateCustomerProtocolValidator() : base("客户协议价")
    {
        RuleFor(x => x.EffectiveStart).NotEmpty().WithMessage("协议生效开始时间不能为空");
        RuleFor(x => x.EffectiveEnd)
            .GreaterThanOrEqualTo(x => x.EffectiveStart)
            .When(x => x.EffectiveEnd.HasValue)
            .WithMessage("协议结束时间不能早于开始时间");
    }
}

public class UpdateCustomerProtocolValidator : NamedCodeValidator<UpdateCustomerProtocolDto>
{
    public UpdateCustomerProtocolValidator() : base("客户协议价")
    {
        RuleForId();
        RuleFor(x => x.EffectiveStart).NotEmpty().WithMessage("协议生效开始时间不能为空");
        RuleFor(x => x.EffectiveEnd)
            .GreaterThanOrEqualTo(x => x.EffectiveStart)
            .When(x => x.EffectiveEnd.HasValue)
            .WithMessage("协议结束时间不能早于开始时间");
    }
}

public class CreateCustomerProtocolGoodsValidator : AbstractValidator<CreateCustomerProtocolGoodsDto>
{
    public CreateCustomerProtocolGoodsValidator()
    {
        RuleFor(x => x.CustomerProtocolId).NotEmpty().WithMessage("客户协议价不能为空");
        RuleFor(x => x.GoodsId).NotEmpty().WithMessage("商品不能为空");
        RuleFor(x => x.GoodsUnitId).NotEmpty().WithMessage("协议价单位不能为空");
        RuleFor(x => x.ProtocolPrice).GreaterThanOrEqualTo(0).WithMessage("协议单价不能小于0");
        RuleFor(x => x.MinOrderQuantity).GreaterThanOrEqualTo(0).When(x => x.MinOrderQuantity.HasValue);
    }
}

public class UpdateCustomerProtocolGoodsValidator : AbstractValidator<UpdateCustomerProtocolGoodsDto>
{
    public UpdateCustomerProtocolGoodsValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("id 必须填写");
        RuleFor(x => x.CustomerProtocolId).NotEmpty().WithMessage("客户协议价不能为空");
        RuleFor(x => x.GoodsId).NotEmpty().WithMessage("商品不能为空");
        RuleFor(x => x.GoodsUnitId).NotEmpty().WithMessage("协议价单位不能为空");
        RuleFor(x => x.ProtocolPrice).GreaterThanOrEqualTo(0).WithMessage("协议单价不能小于0");
        RuleFor(x => x.MinOrderQuantity).GreaterThanOrEqualTo(0).When(x => x.MinOrderQuantity.HasValue);
    }
}

public class CreatePurchaseRuleValidator : NamedCodeValidator<CreatePurchaseRuleDto>
{
    public CreatePurchaseRuleValidator() : base("采购规则")
    {
        RuleFor(x => x.PurchasePattern).InclusiveBetween(1, 2).WithMessage("采购模式只能是 1 或 2");
    }
}

public class UpdatePurchaseRuleValidator : NamedCodeValidator<UpdatePurchaseRuleDto>
{
    public UpdatePurchaseRuleValidator() : base("采购规则")
    {
        RuleForId();
        RuleFor(x => x.PurchasePattern).InclusiveBetween(1, 2).WithMessage("采购模式只能是 1 或 2");
    }
}
