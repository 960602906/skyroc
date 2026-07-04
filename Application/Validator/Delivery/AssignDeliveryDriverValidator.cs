using Application.DTOs.Delivery;
using FluentValidation;

namespace Application.Validator;

/// <summary>
/// 批量分配配送司机请求校验器。
/// </summary>
public class AssignDeliveryDriverValidator : AbstractValidator<AssignDeliveryDriverDto>
{
    /// <summary>
    /// 配置任务集合和司机主键校验规则。
    /// </summary>
    public AssignDeliveryDriverValidator()
    {
        RuleFor(x => x.TaskIds)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("至少选择一个配送任务")
            .Must(ids => ids.Count <= 100).WithMessage("单次最多分配 100 个配送任务")
            .Must(ids => ids.Count == ids.Distinct().Count()).WithMessage("配送任务不能重复");
        RuleForEach(x => x.TaskIds).NotEqual(Guid.Empty).WithMessage("配送任务 ID 不能为空");
        RuleFor(x => x.DriverId).NotEqual(Guid.Empty).WithMessage("司机 ID 不能为空");
    }
}
