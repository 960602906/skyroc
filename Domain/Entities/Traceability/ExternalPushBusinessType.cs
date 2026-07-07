namespace Domain.Entities.Traceability;

/// <summary>
/// 外部报送业务类型，标识报送到外部监管或溯源平台的来源单据种类。
/// </summary>
public enum ExternalPushBusinessType
{
    /// <summary>
    /// 销售订单：按订单维度向外部平台报送交易信息。
    /// </summary>
    SaleOrder = 1,

    /// <summary>
    /// 检测报告：向外部平台报送商品质量检测结果。
    /// </summary>
    InspectionReport = 2,

    /// <summary>
    /// 溯源记录：向外部平台报送商品流通溯源信息。
    /// </summary>
    TraceRecord = 3
}
