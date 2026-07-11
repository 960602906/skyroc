using Domain.Entities.Files;
using Domain.Interfaces;
using Infrastructure.Data;

namespace Infrastructure.Repositories;

/// <summary>
/// 受保护文件元数据仓储，实现已验证文件记录的持久化访问。
/// </summary>
public class StoredFileRepository(ApplicationDbContext context)
    : Repository<StoredFile>(context), IStoredFileRepository
{
}
