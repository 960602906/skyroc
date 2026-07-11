using Domain.Entities.System;

namespace Domain.Interfaces.System;

/// <summary>定义全局运营设置的持久化访问契约。</summary>
public interface ISystemSettingRepository : IRepository<SystemSetting> { }
