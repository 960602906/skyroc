using Application.DTOs.Delivery;
using Application.QueryParameters;
using Shared.Constants;

namespace Application.interfaces;

/// <summary>
/// 配送任务应用服务，提供销售出库生成、订单/司机任务查询、司机分配和路线规划能力。
/// </summary>
public interface IDeliveryTaskService
{
    /// <summary>
    /// 分页查询全部配送订单任务。
    /// </summary>
    /// <param name="parameters">任务分页与组合筛选参数。</param>
    /// <returns>按来源出库时间倒序排列的配送任务。</returns>
    Task<PagedResult<DeliveryTaskDto>> GetOrderTasksAsync(DeliveryTaskQueryParameters parameters);

    /// <summary>
    /// 分页查询已经分配司机的配送任务。
    /// </summary>
    /// <param name="parameters">司机任务分页与组合筛选参数。</param>
    /// <returns>按来源出库时间倒序排列的司机配送任务。</returns>
    Task<PagedResult<DeliveryTaskDto>> GetDriverTasksAsync(DeliveryTaskQueryParameters parameters);

    /// <summary>
    /// 查询配送任务完整详情。
    /// </summary>
    /// <param name="id">配送任务主键。</param>
    /// <returns>配送任务详情。</returns>
    Task<DeliveryTaskDto> GetByIdAsync(Guid id);

    /// <summary>
    /// 从已审核销售出库单幂等生成配送任务，重复调用返回原任务。
    /// </summary>
    /// <param name="stockOutOrderId">已审核销售出库单主键。</param>
    /// <returns>新建或已存在的配送任务。</returns>
    Task<DeliveryTaskDto> GenerateFromStockOutAsync(Guid stockOutOrderId);

    /// <summary>
    /// 为待分配或已分配任务批量指定启用司机，并固化司机及承运商快照。
    /// </summary>
    /// <param name="dto">任务集合和目标司机。</param>
    /// <returns>更新后的配送任务集合。</returns>
    Task<List<DeliveryTaskDto>> AssignDriverAsync(AssignDeliveryDriverDto dto);

    /// <summary>
    /// 按客户已有启用路线批量规划任务，原子更新路线快照和路线内顺序。
    /// </summary>
    /// <param name="dto">待规划任务集合。</param>
    /// <returns>规划后的配送任务集合。</returns>
    Task<List<DeliveryTaskDto>> IntelligentPlanAsync(IntelligentPlanDeliveryTasksDto dto);
}
