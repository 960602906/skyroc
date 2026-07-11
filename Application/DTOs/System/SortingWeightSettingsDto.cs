namespace Application.DTOs.System;

/// <summary>分拣排序的运营权重设置，所有权重均为非负相对值。</summary>
public class SortingWeightSettingsDto
{
    /// <summary>订单创建时间的排序权重。</summary>
    public decimal OrderTimeWeight { get; set; }

    /// <summary>配送路线聚合的排序权重。</summary>
    public decimal RouteWeight { get; set; }

    /// <summary>客户优先级的排序权重。</summary>
    public decimal CustomerWeight { get; set; }
}
