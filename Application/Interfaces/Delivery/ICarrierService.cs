using Application.DTOs.Delivery;
using Application.QueryParameters;

namespace Application.Interfaces;

/// <summary>
///     承运商基础资料维护用例。
/// </summary>
public interface ICarrierService
    : IBaseDataService<CarrierDto, CreateCarrierDto, UpdateCarrierDto, CarrierQueryParameters>;
