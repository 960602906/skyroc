namespace Application.DTOs.Storage;

/// <summary>
///     更新仓库 DTO。
/// </summary>
public class UpdateWareDto : CreateWareDto, IUpdateInput
{
    /// <summary>
    ///     主键 ID。
    /// </summary>
    public Guid Id { get; set; }
}

