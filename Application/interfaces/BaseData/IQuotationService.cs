using Application.DTOs;
using Application.DTOs.Customers;
using Application.DTOs.Goods;
using Application.DTOs.Pricing;
using Application.DTOs.Purchases;
using Application.DTOs.Storage;
using Application.QueryParameters;
using Shared.Constants;

namespace Application.interfaces;

public interface IQuotationService : IBaseDataService<QuotationDto, CreateQuotationDto, UpdateQuotationDto, QuotationQueryParameters>
{
    /// <summary>
    ///     审核或反审核报价单。
    /// </summary>
    Task<QuotationDto> ToggleAuditAsync(Guid id, bool isAudited);
}

