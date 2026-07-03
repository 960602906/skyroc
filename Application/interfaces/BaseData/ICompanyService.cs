using Application.DTOs;
using Application.DTOs.Customers;
using Application.DTOs.Goods;
using Application.DTOs.Pricing;
using Application.DTOs.Purchases;
using Application.DTOs.Storage;
using Application.QueryParameters;
using Shared.Constants;

namespace Application.interfaces;

/// <summary>
/// 定义公司档案的查询和维护用例。
/// </summary>
public interface ICompanyService : IBaseDataService<CompanyDto, CreateCompanyDto, UpdateCompanyDto, CompanyQueryParameters>;
