using Application.Mappers;
using AutoMapper;

namespace SkyRoc.Tests.Mapping;

public sealed class MappingTestFixture
{
    public MappingTestFixture()
    {
        DepartmentMapper = CreateMapper(cfg => cfg.AddProfile<DepartmentMappingProfile>());
        MenuMapper = CreateMapper(cfg =>
        {
            cfg.AddProfile<MenuMappingProfile>();
            cfg.AddProfile<MenuButtonMappingProfile>();
        });
    }

    public IMapper DepartmentMapper { get; }
    public IMapper MenuMapper { get; }

    private static IMapper CreateMapper(Action<IMapperConfigurationExpression> configure)
    {
        var config = new MapperConfiguration(configure);
        return config.CreateMapper();
    }
}
