using Application.DTOs.Delivery;

namespace Application.Validator;

/// <summary>
///     更新司机校验器。
/// </summary>
public class UpdateDriverValidator : NamedCodeValidator<UpdateDriverDto>
{
    /// <summary>
    ///     配置司机更新校验规则，附加主键必填校验。
    /// </summary>
    public UpdateDriverValidator() : base("司机")
    {
        RuleForId();
    }
}
