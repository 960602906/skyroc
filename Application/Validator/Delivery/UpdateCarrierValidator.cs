using Application.DTOs.Delivery;

namespace Application.Validator;

/// <summary>
///     更新承运商校验器。
/// </summary>
public class UpdateCarrierValidator : NamedCodeValidator<UpdateCarrierDto>
{
    /// <summary>
    ///     配置承运商更新校验规则，附加主键必填校验。
    /// </summary>
    public UpdateCarrierValidator() : base("承运商")
    {
        RuleForId();
    }
}
