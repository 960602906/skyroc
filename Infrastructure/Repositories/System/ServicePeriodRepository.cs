using Domain.Entities.System;
using Domain.Interfaces.System;
using Infrastructure.Data;

namespace Infrastructure.Repositories.System;

/// <inheritdoc />
public class ServicePeriodRepository(ApplicationDbContext context) : Repository<ServicePeriod>(context), IServicePeriodRepository { }
