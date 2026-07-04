namespace Application.DTOs.Storage;

/// <summary>
/// 出库商品行编辑请求，携带明细主键以便按行更新草稿出库单。
/// </summary>
public class UpdateStockOutDetailDto : CreateStockOutDetailDto
{
    /// <summary>
    /// 已存在的出库商品行主键；为空表示新增商品行。
    /// </summary>
    public Guid? Id { get; set; }
}
