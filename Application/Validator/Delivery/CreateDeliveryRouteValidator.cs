using Application.DTOs.Delivery;

namespace Application.Validator;

/// <summary>
///     创建配送路线校验器。
/// </summary>
public class CreateDeliveryRouteValidator : NamedCodeValidator<CreateDeliveryRouteDto>
{
    /// <summary>
    ///     配置配送路线创建校验规则。
    /// </summary>
    public CreateDeliveryRouteValidator() : base("配送路线")
    {
    }
}
