using Application.DTOs.Delivery;
using Application.QueryParameters;

namespace Application.Interfaces;

/// <summary>
///     司机基础资料维护用例，创建和更新时校验所属承运商是否存在。
/// </summary>
public interface IDriverService
    : INamedCodeBaseDataService<DriverDto, CreateDriverDto, UpdateDriverDto, DriverQueryParameters>;
