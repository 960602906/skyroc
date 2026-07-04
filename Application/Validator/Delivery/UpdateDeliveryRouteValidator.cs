using Application.DTOs.Delivery;

namespace Application.Validator;

/// <summary>
///     更新配送路线校验器。
/// </summary>
public class UpdateDeliveryRouteValidator : NamedCodeValidator<UpdateDeliveryRouteDto>
{
    /// <summary>
    ///     配置配送路线更新校验规则，附加主键必填校验。
    /// </summary>
    public UpdateDeliveryRouteValidator() : base("配送路线")
    {
        RuleForId();
    }
}
