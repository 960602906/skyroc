using Application.DTOs.Department;
using AutoMapper;
using Domain.Entities;
using Shared.Constants;
using Xunit;

namespace SkyRoc.Tests.Mapping;

[Collection("Mapping")]
public class DepartmentMappingProfileTests
{
    private readonly IMapper _mapper;

    public DepartmentMappingProfileTests(MappingTestFixture fixture)
    {
        _mapper = fixture.DepartmentMapper;
    }

    [Fact]
    public void Should_build_department_tree_from_flat_departments()
    {
        var headquartersId = Guid.NewGuid();
        var engineeringId = Guid.NewGuid();

        var departments = new List<Department>
        {
            new()
            {
                Id = headquartersId,
                Name = "总部",
                Code = "HQ",
                Sort = 1,
                Status = Status.Enable,
                CreateTime = DateTime.UtcNow
            },
            new()
            {
                Id = engineeringId,
                Name = "研发部",
                Code = "ENGINEERING",
                ParentId = headquartersId,
                Sort = 1,
                Status = Status.Enable,
                CreateTime = DateTime.UtcNow
            }
        };

        var result = _mapper.Map<List<DepartmentTreeDto>>(departments);

        var root = Assert.Single(result);
        Assert.Equal(headquartersId, root.Id);
        Assert.NotNull(root.Children);
        var child = Assert.Single(root.Children!);
        Assert.Equal(engineeringId, child.Id);
        Assert.Equal(headquartersId, child.ParentId);
    }

    [Fact]
    public void Should_not_assign_children_for_leaf_department()
    {
        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = "独立部门",
            Code = "SOLO",
            Sort = 10,
            Status = Status.Enable,
            CreateTime = DateTime.UtcNow
        };

        var result = _mapper.Map<DepartmentTreeDto>(department);

        Assert.Null(result.Children);
    }
}
