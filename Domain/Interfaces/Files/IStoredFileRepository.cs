using Domain.Entities.Files;

namespace Domain.Interfaces;

/// <summary>
/// 受保护文件元数据仓储，负责按文件主键读取已验证的存储记录。
/// </summary>
public interface IStoredFileRepository : IRepository<StoredFile>
{
}
