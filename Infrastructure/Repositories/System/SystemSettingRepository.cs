using Domain.Entities.System;
using Domain.Interfaces.System;
using Infrastructure.Data;

namespace Infrastructure.Repositories.System;

/// <inheritdoc />
public class SystemSettingRepository(ApplicationDbContext context) : Repository<SystemSetting>(context), ISystemSettingRepository { }
