using Domain.Entities.System;
using Domain.Interfaces.System;
using Infrastructure.Data;

namespace Infrastructure.Repositories.System;

/// <inheritdoc />
public class NoticeRepository(ApplicationDbContext context) : Repository<Notice>(context), INoticeRepository { }
