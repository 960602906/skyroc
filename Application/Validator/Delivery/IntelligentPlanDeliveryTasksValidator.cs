using Application.DTOs.Delivery;
using FluentValidation;

namespace Application.Validator;

/// <summary>
/// 配送任务智能路线规划请求校验器。
/// </summary>
public class IntelligentPlanDeliveryTasksValidator : AbstractValidator<IntelligentPlanDeliveryTasksDto>
{
    /// <summary>
    /// 配置任务集合非空、有效和去重规则。
    /// </summary>
    public IntelligentPlanDeliveryTasksValidator()
    {
        RuleFor(x => x.TaskIds)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("至少选择一个配送任务")
            .Must(ids => ids.Count <= 100).WithMessage("单次最多规划 100 个配送任务")
            .Must(ids => ids.Count == ids.Distinct().Count()).WithMessage("配送任务不能重复");
        RuleForEach(x => x.TaskIds).NotEqual(Guid.Empty).WithMessage("配送任务 ID 不能为空");
    }
}
