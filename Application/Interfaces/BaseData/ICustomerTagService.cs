using Application.DTOs;
using Application.DTOs.Customers;
using Application.DTOs.Goods;
using Application.DTOs.Pricing;
using Application.DTOs.Purchases;
using Application.DTOs.Storage;
using Application.QueryParameters;
using Shared.Constants;

namespace Application.Interfaces;

/// <summary>
/// 定义客户标签树及客户标签关系的维护用例。
/// </summary>
public interface ICustomerTagService : ITreeBaseDataService<CustomerTagDto, CreateCustomerTagDto, UpdateCustomerTagDto, CustomerTagQueryParameters>;
