using Domain.Entities.Delivery;
using Domain.Interfaces;
using Infrastructure.Data;

namespace Infrastructure.Repositories;

/// <summary>
///     承运商仓储。
/// </summary>
public class CarrierRepository(ApplicationDbContext context)
    : NamedCodeRepository<Carrier>(context), ICarrierRepository;
