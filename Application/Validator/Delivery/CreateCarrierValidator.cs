using Application.DTOs.Delivery;

namespace Application.Validator;

/// <summary>
///     创建承运商校验器。
/// </summary>
public class CreateCarrierValidator : NamedCodeValidator<CreateCarrierDto>
{
    /// <summary>
    ///     配置承运商创建校验规则。
    /// </summary>
    public CreateCarrierValidator() : base("承运商")
    {
    }
}
