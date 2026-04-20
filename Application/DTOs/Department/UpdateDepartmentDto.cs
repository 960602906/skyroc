namespace Application.DTOs.Department;

public class UpdateDepartmentDto: CreateDepartmentDto
{
    /// <summary>
    ///     主键ID
    /// </summary>
    public Guid Id { get; set; }
}