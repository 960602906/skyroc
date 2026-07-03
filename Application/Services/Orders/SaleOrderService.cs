using Application.DTOs.Orders;
using Application.Exceptions;
using Application.Extensions;
using Application.interfaces;
using Application.QueryParameters;
using AutoMapper;
using Domain.Entities.Customers;
using Domain.Entities.Goods;
using Domain.Entities.Orders;
using Domain.Interfaces;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Shared.Constants;
using ValidationException = Application.Exceptions.ValidationException;

namespace Application.Services;

/// <summary>
/// 销售订单 CRUD 服务。
/// </summary>
public class SaleOrderService(
    ISaleOrderRepository saleOrderRepository,
    ISaleOrderDetailRepository saleOrderDetailRepository,
    IOrderAuditLogRepository orderAuditLogRepository,
    ICustomerRepository customerRepository,
    IQuotationRepository quotationRepository,
    IWareRepository wareRepository,
    IGoodsRepository goodsRepository,
    IGoodsUnitRepository goodsUnitRepository,
    IUnitOfWork unitOfWork,
    IMapper mapper,
    ICurrentUserService currentUserService,
    IValidator<CreateSaleOrderDto> createValidator,
    IValidator<UpdateSaleOrderDto> updateValidator,
    ILogger<SaleOrderService> logger) : ISaleOrderService
{
    /// <inheritdoc />
    public async Task<PagedResult<SaleOrderDto>> GetPagedAsync(SaleOrderQueryParameters parameters)
    {
        var result = await saleOrderRepository.GetPagedAsync(
            parameters.QueryBuild(),
            parameters.Current,
            parameters.Size,
            x => x.OrderDate,
            true);
        return mapper.ToPagedResult<SaleOrder, SaleOrderDto>(result, parameters);
    }

    /// <inheritdoc />
    public async Task<SaleOrderDto> GetByIdAsync(Guid id)
    {
        var order = await GetRequiredOrderAsync(id);
        return mapper.Map<SaleOrderDto>(order);
    }

    /// <inheritdoc />
    public async Task<SaleOrderDto> CreateAsync(CreateSaleOrderDto dto)
    {
        await ValidateAsync(createValidator, dto);
        var customer = await GetRequiredCustomerAsync(dto.CustomerId);
        await ValidateOrderReferencesAsync(dto.QuotationId, dto.WareId);

        var order = mapper.Map<SaleOrder>(dto);
        order.Id = Guid.NewGuid();
        order.OrderNo = await GenerateOrderNoAsync();
        ApplyCustomerSnapshot(order, customer);
        ApplyCreateAudit(order);

        order.Details.Clear();
        foreach (var detailDto in dto.Details)
        {
            var detail = await BuildDetailAsync(
                order.Id,
                detailDto.GoodsId,
                detailDto.GoodsUnitId,
                detailDto.Quantity,
                detailDto.FixedPrice,
                detailDto.FixedGoodsUnitId,
                detailDto.Remark,
                detailDto.InnerRemark);
            ApplyCreateAudit(detail);
            order.Details.Add(detail);
        }

        RecalculateOrder(order);
        await ExecuteInTransactionAsync(async () =>
        {
            await saleOrderRepository.AddAsync(order);
            await orderAuditLogRepository.AddAsync(CreateAuditLog(
                order.Id,
                OrderAuditAction.Submit,
                SaleOrderStatus.PendingAudit,
                SaleOrderStatus.PendingAudit,
                null));
        });

        logger.LogInformation("销售订单创建成功: {OrderId}, {OrderNo}", order.Id, order.OrderNo);
        return mapper.Map<SaleOrderDto>(await GetRequiredOrderAsync(order.Id));
    }

    /// <inheritdoc />
    public async Task<SaleOrderDto> UpdateAsync(UpdateSaleOrderDto dto)
    {
        await ValidateAsync(updateValidator, dto);
        var order = await GetRequiredOrderAsync(dto.Id);
        var customer = await GetRequiredCustomerAsync(dto.CustomerId);
        await ValidateOrderReferencesAsync(dto.QuotationId, dto.WareId);

        var existingDetails = order.Details.ToDictionary(x => x.Id);
        var requestedIds = dto.Details
            .Where(x => x.Id.HasValue)
            .Select(x => x.Id!.Value)
            .ToList();
        if (requestedIds.Count != requestedIds.Distinct().Count())
        {
            throw new BusinessException("订单明细 ID 不能重复");
        }

        var invalidIds = requestedIds.Where(id => !existingDetails.ContainsKey(id)).ToList();
        if (invalidIds.Count > 0)
        {
            throw new BusinessException("部分订单明细不存在或不属于当前订单");
        }

        var preparedDetails = new List<(UpdateSaleOrderDetailDto Input, SaleOrderDetail Detail)>();
        foreach (var detailDto in dto.Details)
        {
            var detail = await BuildDetailAsync(
                order.Id,
                detailDto.GoodsId,
                detailDto.GoodsUnitId,
                detailDto.Quantity,
                detailDto.FixedPrice,
                detailDto.FixedGoodsUnitId,
                detailDto.Remark,
                detailDto.InnerRemark);
            preparedDetails.Add((detailDto, detail));
        }

        await ExecuteInTransactionAsync(async () =>
        {
            ApplyEditableFields(order, dto);
            ApplyCustomerSnapshot(order, customer);
            ApplyUpdateAudit(order);
            order.UpdateStatus = true;

            var removedDetails = order.Details.Where(x => !requestedIds.Contains(x.Id)).ToList();
            if (removedDetails.Count > 0)
            {
                await saleOrderDetailRepository.DeleteRangeAsync(removedDetails);
                foreach (var removedDetail in removedDetails)
                {
                    order.Details.Remove(removedDetail);
                }
            }

            foreach (var (input, preparedDetail) in preparedDetails)
            {
                if (input.Id.HasValue)
                {
                    var existingDetail = existingDetails[input.Id.Value];
                    ApplyEditableFields(existingDetail, preparedDetail);
                    ApplyUpdateAudit(existingDetail);
                    await saleOrderDetailRepository.UpdateAsync(existingDetail);
                }
                else
                {
                    ApplyCreateAudit(preparedDetail);
                    order.Details.Add(preparedDetail);
                    await saleOrderDetailRepository.AddAsync(preparedDetail);
                }
            }

            RecalculateOrder(order);
            await saleOrderRepository.UpdateAsync(order);
        });

        logger.LogInformation("销售订单更新成功: {OrderId}, {OrderNo}", order.Id, order.OrderNo);
        return mapper.Map<SaleOrderDto>(await GetRequiredOrderAsync(order.Id));
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid id)
    {
        var order = await GetRequiredOrderAsync(id);
        await ExecuteInTransactionAsync(async () => await saleOrderRepository.DeleteAsync(order));
        logger.LogInformation("销售订单删除成功: {OrderId}, {OrderNo}", order.Id, order.OrderNo);
        return true;
    }

    /// <inheritdoc />
    public Task<SaleOrderDto> ApproveAsync(Guid id, string? remark)
    {
        return TransitionAsync(
            id,
            OrderAuditAction.Approve,
            SaleOrderStatus.PendingAudit,
            SaleOrderStatus.SortingPending,
            remark);
    }

    /// <inheritdoc />
    public Task<SaleOrderDto> RejectAsync(Guid id, string? remark)
    {
        return TransitionAsync(
            id,
            OrderAuditAction.Reject,
            SaleOrderStatus.PendingAudit,
            SaleOrderStatus.Rejected,
            remark);
    }

    /// <inheritdoc />
    public Task<SaleOrderDto> ResubmitAsync(Guid id, string? remark)
    {
        return TransitionAsync(
            id,
            OrderAuditAction.Resubmit,
            SaleOrderStatus.Rejected,
            SaleOrderStatus.PendingAudit,
            remark);
    }

    private async Task<SaleOrderDto> TransitionAsync(
        Guid id,
        OrderAuditAction action,
        SaleOrderStatus requiredStatus,
        SaleOrderStatus targetStatus,
        string? remark)
    {
        var order = await GetRequiredOrderAsync(id);
        if (order.OrderStatus != requiredStatus)
        {
            throw new BusinessException(
                $"订单状态为 {order.OrderStatus}，不能执行 {GetActionName(action)} 操作");
        }

        var previousStatus = order.OrderStatus;
        await ExecuteInTransactionAsync(async () =>
        {
            order.OrderStatus = targetStatus;
            ApplyUpdateAudit(order);
            await orderAuditLogRepository.AddAsync(CreateAuditLog(
                order.Id,
                action,
                previousStatus,
                targetStatus,
                remark));
        });

        logger.LogInformation(
            "销售订单状态流转成功: {OrderId}, {Action}, {PreviousStatus} -> {CurrentStatus}",
            order.Id,
            action,
            previousStatus,
            targetStatus);
        return mapper.Map<SaleOrderDto>(await GetRequiredOrderAsync(order.Id));
    }

    private static async Task ValidateAsync<T>(IValidator<T> validator, T dto)
    {
        var result = await validator.ValidateAsync(dto);
        if (!result.IsValid)
        {
            throw new ValidationException(result.Errors);
        }
    }

    private async Task<SaleOrder> GetRequiredOrderAsync(Guid id)
    {
        return await saleOrderRepository.GetByIdAsync(id)
               ?? throw new NotFoundException("销售订单不存在");
    }

    private async Task<Customer> GetRequiredCustomerAsync(Guid id)
    {
        return await customerRepository.GetByIdAsync(id)
               ?? throw new BusinessException("客户不存在");
    }

    private async Task ValidateOrderReferencesAsync(Guid? quotationId, Guid? wareId)
    {
        if (quotationId.HasValue && !await quotationRepository.ExistsAsync(quotationId.Value))
        {
            throw new BusinessException("报价单不存在");
        }

        if (wareId.HasValue && !await wareRepository.ExistsAsync(wareId.Value))
        {
            throw new BusinessException("仓库不存在");
        }
    }

    private async Task<SaleOrderDetail> BuildDetailAsync(
        Guid orderId,
        Guid goodsId,
        Guid goodsUnitId,
        decimal quantity,
        decimal fixedPrice,
        Guid fixedGoodsUnitId,
        string? remark,
        string? innerRemark)
    {
        var goods = await goodsRepository.GetByIdAsync(goodsId)
                    ?? throw new BusinessException("商品不存在");
        var goodsUnit = await goodsUnitRepository.GetByIdAsync(goodsUnitId);
        if (goodsUnit is null || goodsUnit.GoodsId != goods.Id)
        {
            throw new BusinessException($"下单单位不属于商品 {goods.Name}");
        }

        var fixedGoodsUnit = await goodsUnitRepository.GetByIdAsync(fixedGoodsUnitId);
        if (fixedGoodsUnit is null || fixedGoodsUnit.GoodsId != goods.Id)
        {
            throw new BusinessException($"单价单位不属于商品 {goods.Name}");
        }

        if (goodsUnit.ConversionRate <= 0 || fixedGoodsUnit.ConversionRate <= 0)
        {
            throw new BusinessException($"商品 {goods.Name} 的单位换算比例必须大于0");
        }

        var baseUnit = goods.BaseUnit;
        if (baseUnit is null)
        {
            throw new BusinessException($"商品 {goods.Name} 未配置基础单位");
        }

        var baseQuantity = decimal.Round(
            quantity * goodsUnit.ConversionRate,
            6,
            MidpointRounding.AwayFromZero);
        var totalPrice = decimal.Round(
            baseQuantity / fixedGoodsUnit.ConversionRate * fixedPrice,
            4,
            MidpointRounding.AwayFromZero);

        return new SaleOrderDetail
        {
            Id = Guid.NewGuid(),
            SaleOrderId = orderId,
            GoodsId = goods.Id,
            GoodsNameSnapshot = goods.Name,
            GoodsCodeSnapshot = goods.Code,
            GoodsImageSnapshot = goods.Images
                .OrderByDescending(x => x.IsPrimary)
                .ThenBy(x => x.Sort)
                .Select(x => x.Url)
                .FirstOrDefault(),
            GoodsTypeNameSnapshot = goods.GoodsType.Name,
            GoodsDescriptionSnapshot = goods.Description,
            GoodsUnitId = goodsUnit.Id,
            GoodsUnitNameSnapshot = goodsUnit.Name,
            Quantity = quantity,
            BaseQuantity = baseQuantity,
            BaseUnitId = baseUnit.Id,
            BaseUnitNameSnapshot = baseUnit.Name,
            UnitConversion = goodsUnit.ConversionRate,
            FixedPrice = fixedPrice,
            FixedGoodsUnitId = fixedGoodsUnit.Id,
            FixedGoodsUnitNameSnapshot = fixedGoodsUnit.Name,
            TotalPrice = totalPrice,
            Remark = remark,
            InnerRemark = innerRemark
        };
    }

    private async Task<string> GenerateOrderNoAsync()
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var suffix = Guid.NewGuid().ToString("N")[..12].ToUpperInvariant();
            var orderNo = $"SO{DateTime.UtcNow:yyyyMMddHHmmssfff}{suffix}";
            if (!await saleOrderRepository.ExistsOrderNoAsync(orderNo))
            {
                return orderNo;
            }
        }

        throw new BusinessException("订单号生成失败，请重试");
    }

    private async Task ExecuteInTransactionAsync(Func<Task> action)
    {
        await unitOfWork.BeginTransactionAsync();
        try
        {
            await action();
            await unitOfWork.CommitTransactionAsync();
        }
        catch
        {
            if (unitOfWork.HasActiveTransaction)
            {
                await unitOfWork.RollbackTransactionAsync();
            }

            throw;
        }
    }

    private void ApplyCreateAudit(Domain.Entities.BaseEntity entity)
    {
        entity.CreateBy = currentUserService.GetUserId();
        entity.CreateName = currentUserService.GetUserName();
    }

    private void ApplyUpdateAudit(Domain.Entities.BaseEntity entity)
    {
        entity.UpdateBy = currentUserService.GetUserId();
        entity.UpdateName = currentUserService.GetUserName();
    }

    private OrderAuditLog CreateAuditLog(
        Guid orderId,
        OrderAuditAction action,
        SaleOrderStatus previousStatus,
        SaleOrderStatus currentStatus,
        string? remark)
    {
        var auditLog = new OrderAuditLog
        {
            Id = Guid.NewGuid(),
            SaleOrderId = orderId,
            Action = action,
            PreviousStatus = previousStatus,
            CurrentStatus = currentStatus,
            AuditUserId = currentUserService.GetUserId(),
            AuditUserNameSnapshot = currentUserService.GetUserName() ?? string.Empty,
            AuditTime = DateTime.UtcNow,
            Remark = string.IsNullOrWhiteSpace(remark) ? null : remark.Trim()
        };
        ApplyCreateAudit(auditLog);
        return auditLog;
    }

    private static string GetActionName(OrderAuditAction action)
    {
        return action switch
        {
            OrderAuditAction.Submit => "提交",
            OrderAuditAction.Approve => "通过",
            OrderAuditAction.Reject => "驳回",
            OrderAuditAction.Resubmit => "重提",
            _ => action.ToString()
        };
    }

    private static void ApplyCustomerSnapshot(SaleOrder order, Customer customer)
    {
        order.CustomerId = customer.Id;
        order.CustomerNameSnapshot = customer.Name;
        order.CustomerCodeSnapshot = customer.Code;
    }

    private static void ApplyEditableFields(SaleOrder order, UpdateSaleOrderDto dto)
    {
        order.CustomerId = dto.CustomerId;
        order.QuotationId = dto.QuotationId;
        order.WareId = dto.WareId;
        order.OrderDate = dto.OrderDate;
        order.ReceiveDate = dto.ReceiveDate;
        order.ContactNameSnapshot = dto.ContactName;
        order.ContactPhoneSnapshot = dto.ContactPhone;
        order.DeliveryAddressSnapshot = dto.DeliveryAddress;
        order.Remark = dto.Remark;
        order.InnerRemark = dto.InnerRemark;
    }

    private static void ApplyEditableFields(SaleOrderDetail target, SaleOrderDetail source)
    {
        target.GoodsId = source.GoodsId;
        target.GoodsNameSnapshot = source.GoodsNameSnapshot;
        target.GoodsCodeSnapshot = source.GoodsCodeSnapshot;
        target.GoodsImageSnapshot = source.GoodsImageSnapshot;
        target.GoodsTypeNameSnapshot = source.GoodsTypeNameSnapshot;
        target.GoodsDescriptionSnapshot = source.GoodsDescriptionSnapshot;
        target.GoodsUnitId = source.GoodsUnitId;
        target.GoodsUnitNameSnapshot = source.GoodsUnitNameSnapshot;
        target.Quantity = source.Quantity;
        target.BaseQuantity = source.BaseQuantity;
        target.BaseUnitId = source.BaseUnitId;
        target.BaseUnitNameSnapshot = source.BaseUnitNameSnapshot;
        target.UnitConversion = source.UnitConversion;
        target.FixedPrice = source.FixedPrice;
        target.FixedGoodsUnitId = source.FixedGoodsUnitId;
        target.FixedGoodsUnitNameSnapshot = source.FixedGoodsUnitNameSnapshot;
        target.TotalPrice = source.TotalPrice;
        target.Remark = source.Remark;
        target.InnerRemark = source.InnerRemark;
    }

    private static void RecalculateOrder(SaleOrder order)
    {
        order.OrderPrice = order.Details.Sum(x => x.TotalPrice);
        order.SettlementPrice = order.OrderPrice;
    }
}
