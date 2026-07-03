using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;

namespace Infrastructure.Repositories;

/// <inheritdoc />
public class OperationLogRepository(ApplicationDbContext context) : Repository<OperationLog>(context), IOperationLogRepository
{

}
