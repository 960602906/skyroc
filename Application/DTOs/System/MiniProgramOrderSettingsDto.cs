namespace Application.DTOs.System;

/// <summary>小程序下单的全局运营设置。</summary>
public class MiniProgramOrderSettingsDto
{
    /// <summary>是否允许小程序提交新订单。</summary>
    public bool IsEnabled { get; set; }

    /// <summary>允许提前下单的自然日天数，范围为 0 至 30 天。</summary>
    public int MaxAdvanceOrderDays { get; set; }
}
