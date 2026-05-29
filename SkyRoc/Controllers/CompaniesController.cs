using Application.DTOs.Customers;
using Application.interfaces;
using Application.QueryParameters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SkyRoc.Controllers;

/// <summary>
///     公司管理控制器。
/// </summary>
[Route("api/[controller]")]
[Authorize]
public class CompaniesController(ICompanyService service)
    : BaseDataController<CompanyDto, CreateCompanyDto, UpdateCompanyDto, CompanyQueryParameters>(service);
