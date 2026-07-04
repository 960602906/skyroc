using Application.DTOs.Delivery;

namespace Application.Validator;

/// <summary>
///     创建司机校验器。
/// </summary>
public class CreateDriverValidator : NamedCodeValidator<CreateDriverDto>
{
    /// <summary>
    ///     配置司机创建校验规则。
    /// </summary>
    public CreateDriverValidator() : base("司机")
    {
    }
}
