using Application.DTOs.Delivery;
using FluentValidation;

namespace Application.Validator;

/// <summary>
/// 回单归档请求校验器。
/// </summary>
public class ReturnOrderReceiptValidator : AbstractValidator<ReturnOrderReceiptDto>
{
    /// <summary>
    /// 配置回单资料地址和归档说明长度校验。
    /// </summary>
    public ReturnOrderReceiptValidator()
    {
        RuleFor(x => x.ReceiptImageUrl)
            .NotEmpty().WithMessage("回单资料地址不能为空")
            .MaximumLength(1000).WithMessage("回单资料地址不能超过 1000 个字符")
            .Must(BeHttpUrl).WithMessage("回单资料地址必须使用 HTTP 或 HTTPS 协议");
        RuleFor(x => x.Remark).MaximumLength(500).WithMessage("回单说明不能超过 500 个字符");
    }

    private static bool BeHttpUrl(string value)
    {
        return Uri.TryCreate(value?.Trim(), UriKind.Absolute, out var uri)
               && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
