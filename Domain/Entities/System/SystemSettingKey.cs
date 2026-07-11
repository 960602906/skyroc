namespace Domain.Entities.System;

/// <summary>
/// 可由系统支撑模块维护的全局运营设置键。
/// </summary>
public enum SystemSettingKey
{
    /// <summary>小程序下单开关及其提前下单限制。</summary>
    MiniProgramOrder = 1,

    /// <summary>分拣排序使用的权重参数。</summary>
    SortingWeight = 2
}
