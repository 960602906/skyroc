namespace Application.DTOs.Delivery;

/// <summary>
/// 配送任务智能路线规划请求，按客户已配置的启用路线批量匹配。
/// </summary>
public class IntelligentPlanDeliveryTasksDto
{
    /// <summary>
    /// 待规划配送任务主键集合，所有任务必须存在且客户已配置启用路线。
    /// </summary>
    public List<Guid> TaskIds { get; set; } = [];
}
