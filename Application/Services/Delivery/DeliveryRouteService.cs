using System.Linq.Expressions;
using Application.DTOs.Delivery;
using Application.Exceptions;
using Application.Interfaces;
using Application.QueryParameters;
using AutoMapper;
using Domain.Entities.Delivery;
using Domain.Interfaces;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Application.Extensions;

namespace Application.Services;

/// <inheritdoc cref="IDeliveryRouteService" />
public class DeliveryRouteService(
    IDeliveryRouteRepository repository,
    ICustomerRepository customerRepository,
    IUnitOfWork unitOfWork,
    ILogger<DeliveryRouteService> logger,
    IMapper mapper,
    ICurrentUserService currentUserService,
    IValidator<CreateDeliveryRouteDto> createValidator,
    IValidator<UpdateDeliveryRouteDto> updateValidator)
    : NamedCodeBaseDataService<DeliveryRoute, DeliveryRouteDto, CreateDeliveryRouteDto, UpdateDeliveryRouteDto,
            DeliveryRouteQueryParameters>(
            repository, unitOfWork, logger, mapper, currentUserService, createValidator, updateValidator),
        IDeliveryRouteService
{
    /// <inheritdoc />
    protected override string DisplayName => "配送路线";

    /// <inheritdoc />
    protected override Expression<Func<DeliveryRoute, bool>> BuildPredicate(DeliveryRouteQueryParameters parameters)
    {
        return parameters.QueryBuild();
    }

    /// <inheritdoc />
    protected override async Task ValidateCreateAsync(CreateDeliveryRouteDto dto)
    {
        await base.ValidateCreateAsync(dto);
        await ValidateCustomersAsync(dto.CustomerIds);
    }

    /// <inheritdoc />
    protected override async Task ValidateUpdateAsync(Guid id, UpdateDeliveryRouteDto dto)
    {
        await base.ValidateUpdateAsync(id, dto);
        await ValidateCustomersAsync(dto.CustomerIds);
    }

    /// <inheritdoc />
    protected override async Task AfterCreateAsync(DeliveryRoute entity, CreateDeliveryRouteDto dto)
    {
        await repository.ReplaceCustomerRelationsAsync(entity.Id, dto.CustomerIds);
    }

    /// <inheritdoc />
    protected override async Task AfterUpdateAsync(DeliveryRoute entity, UpdateDeliveryRouteDto dto)
    {
        await repository.ReplaceCustomerRelationsAsync(entity.Id, dto.CustomerIds);
    }

    /// <inheritdoc />
    public async Task<DeliveryRouteDto> DispatchCustomersAsync(Guid routeId, List<Guid>? customerIds)
    {
        var route = await Repository.GetByIdAsync(routeId);
        if (route is null)
        {
            throw new NotFoundException($"{DisplayName}不存在");
        }

        var normalizedIds = NormalizeCustomerIds(customerIds);
        await ValidateCustomersAsync(normalizedIds);
        await repository.ReplaceCustomerRelationsAsync(routeId, normalizedIds);
        route.ApplyUpdateAudit(CurrentUserService);
        await Repository.UpdateAsync(route);
        await UnitOfWork.SaveChangesAsync();
        Logger.LogInformation("{DisplayName}客户分配成功: {Id}", DisplayName, routeId);

        // 直接以规范化后的目标客户集合构造响应，避免复用已随删除失效的跟踪导航导致返回过期客户。
        var dto = Mapper.Map<DeliveryRouteDto>(route);
        dto.CustomerIds = normalizedIds;
        return dto;
    }

    private static List<Guid> NormalizeCustomerIds(IEnumerable<Guid>? customerIds)
    {
        return customerIds?.Where(x => x != Guid.Empty).Distinct().ToList() ?? [];
    }

    private async Task ValidateCustomersAsync(IEnumerable<Guid>? customerIds)
    {
        var ids = NormalizeCustomerIds(customerIds);
        if (ids.Count == 0)
        {
            return;
        }

        var customers = await customerRepository.GetByIdsAsync(ids);
        if (customers.Count != ids.Count)
        {
            throw new BusinessException("部分客户不存在");
        }
    }
}
