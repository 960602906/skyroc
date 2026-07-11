using Domain.Entities.System;

namespace Domain.Interfaces.System;

/// <summary>定义登录审计记录的持久化访问契约。</summary>
public interface ILoginLogRepository : IRepository<LoginLog> { }
