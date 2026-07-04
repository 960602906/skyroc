using Domain.Entities.Delivery;

namespace Domain.Interfaces;

/// <summary>
///     配送异常仓储接口。
/// </summary>
public interface IDeliveryExceptionRepository : IRepository<DeliveryException>
{
    /// <summary>
    ///     按异常编号判断是否存在，可排除指定记录。
    /// </summary>
    /// <param name="exceptionNo">配送异常业务编号。</param>
    /// <param name="excludeId">需要排除的记录主键，通常为当前更新记录。</param>
    /// <returns>存在返回 true，否则返回 false。</returns>
    Task<bool> ExistsByExceptionNoAsync(string exceptionNo, Guid? excludeId = null);
}
