using Application.DTOs.AfterSales;
using Application.QueryParameters.AfterSales;
using Shared.Constants;

namespace Application.Interfaces;

/// <summary>
/// 售后取货任务应用服务，提供查询、司机分配和严格的取货执行状态流转。
/// </summary>
public interface IPickupTaskService
{
    /// <summary>分页查询售后取货任务及其退货入库衔接状态。</summary>
    Task<PagedResult<PickupTaskDto>> GetPagedAsync(PickupTaskQueryParameters parameters);

    /// <summary>按主键查询取货任务的售后来源、商品、调度和履约详情。</summary>
    Task<PickupTaskDto> GetByIdAsync(Guid id);

    /// <summary>为尚未开始的取货任务分配或更换启用司机。</summary>
    Task<PickupTaskDto> AssignAsync(Guid id, AssignPickupTaskDto dto);

    /// <summary>将已分配任务从待取货推进为取货中，并记录开始时间。</summary>
    Task<PickupTaskDto> StartAsync(Guid id);

    /// <summary>将取货中的任务标记为已完成，使其可用于生成销售退货入库。</summary>
    Task<PickupTaskDto> CompleteAsync(Guid id);
}
