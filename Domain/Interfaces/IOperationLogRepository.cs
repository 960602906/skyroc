using Domain.Entities;

namespace Domain.Interfaces;

/// <summary>
/// 定义操作日志的写入和查询操作。
/// </summary>
public interface IOperationLogRepository : IRepository<OperationLog>
{

}
