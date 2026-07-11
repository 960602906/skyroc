using Domain.Entities.System;
using Domain.Interfaces.System;
using Infrastructure.Data;

namespace Infrastructure.Repositories.System;

/// <inheritdoc />
public class LoginLogRepository(ApplicationDbContext context) : Repository<LoginLog>(context), ILoginLogRepository { }
