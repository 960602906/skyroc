namespace Domain.Entities.System;

/// <summary>
/// 系统级运营配置，以稳定键和值 JSON 保存少量全局单例设置。
/// </summary>
public class SystemSetting : BaseEntity
{
    /// <summary>设置的稳定键，同一键仅允许一条当前生效记录。</summary>
    public SystemSettingKey SettingKey { get; set; }

    /// <summary>设置值 JSON，仅由对应的强类型应用服务反序列化和校验。</summary>
    public string SettingValue { get; set; } = string.Empty;
}
