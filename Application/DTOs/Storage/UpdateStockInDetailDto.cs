namespace Application.DTOs.Storage;

/// <summary>
/// 入库商品行编辑请求，携带明细主键以便按行更新草稿入库单。
/// </summary>
public class UpdateStockInDetailDto : CreateStockInDetailDto
{
    /// <summary>
    /// 已存在的入库商品行主键；为空表示新增商品行。
    /// </summary>
    public Guid? Id { get; set; }
}
