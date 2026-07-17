using Application.DTOs.Delivery;
using Application.QueryParameters;
using Shared.Constants;

namespace Application.Interfaces;

/// <summary>
/// 配送异常应用服务，提供异常登记、查询和处理闭环能力。
/// </summary>
public interface IDeliveryExceptionService
{
    /// <summary>
    /// 分页查询配送异常及其任务、司机和客户信息。
    /// </summary>
    /// <param name="parameters">异常分页与组合筛选参数。</param>
    /// <returns>按登记时间倒序排列的配送异常。</returns>
    Task<PagedResult<DeliveryExceptionDto>> GetPagedAsync(DeliveryExceptionQueryParameters parameters);

    /// <summary>
    /// 查询配送异常详情。
    /// </summary>
    /// <param name="id">配送异常主键。</param>
    /// <returns>配送异常详情。</returns>
    Task<DeliveryExceptionDto> GetByIdAsync(Guid id);

    /// <summary>
    /// 为已分配且尚未签收的任务登记异常，并将任务原子标记为配送异常。
    /// </summary>
    /// <param name="dto">异常所属任务和事实描述。</param>
    /// <returns>新登记的配送异常。</returns>
    Task<DeliveryExceptionDto> CreateAsync(CreateDeliveryExceptionDto dto);

    /// <summary>
    /// 完成待处理配送异常；任务没有其他待处理异常时恢复为已分配或配送中。
    /// </summary>
    /// <param name="id">配送异常主键。</param>
    /// <param name="dto">异常处理动作与结果。</param>
    /// <returns>处理完成后的配送异常。</returns>
    Task<DeliveryExceptionDto> HandleAsync(Guid id, HandleDeliveryExceptionDto dto);
}
