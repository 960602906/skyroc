using Domain.Entities.Delivery;

namespace Domain.Interfaces;

/// <summary>
///     司机仓储接口，可按主键预加载所属承运商。
/// </summary>
public interface IDriverRepository : INamedCodeRepository<Driver>
{
}
