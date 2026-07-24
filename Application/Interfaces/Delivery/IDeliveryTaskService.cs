using Application.DTOs.Delivery;
using Application.QueryParameters;
using Shared.Constants;

namespace Application.Interfaces;

/// <summary>
/// 配送任务应用服务，提供任务生成、调度、配送执行、签收验收和回单归档能力。
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
    /// 根据配送任务号查询配送任务完整详情。
    /// </summary>
    /// <param name="taskNo">配送任务号。</param>
    /// <returns>配送任务详情。</returns>
    /// <exception cref="Application.Exceptions.BusinessException">配送任务号为空时抛出。</exception>
    /// <exception cref="Application.Exceptions.NotFoundException">配送任务不存在时抛出。</exception>
    Task<DeliveryTaskDto> GetByTaskNoAsync(string taskNo);

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

    /// <summary>
    /// 将已分配任务推进到配送中，并同步销售订单进入配送中状态。
    /// </summary>
    /// <param name="id">配送任务主键。</param>
    /// <returns>开始执行后的配送任务。</returns>
    Task<DeliveryTaskDto> StartDeliveryAsync(Guid id);

    /// <summary>
    /// 签收配送中任务，保存本次全部出库商品验收明细，并在全部任务完成后同步整单状态与结算金额。
    /// </summary>
    /// <param name="id">配送任务主键。</param>
    /// <param name="dto">客户签收人与商品验收结果。</param>
    /// <returns>新生成的签收回单。</returns>
    Task<OrderReceiptDto> SignAsync(Guid id, SignDeliveryTaskDto dto);

    /// <summary>
    /// 归档已签收任务的回单资料，并在全部回单完成后同步销售订单回单状态。
    /// </summary>
    /// <param name="id">配送任务主键。</param>
    /// <param name="dto">回单资料地址与归档说明。</param>
    /// <returns>归档后的签收回单。</returns>
    Task<OrderReceiptDto> ReturnReceiptAsync(Guid id, ReturnOrderReceiptDto dto);
}
