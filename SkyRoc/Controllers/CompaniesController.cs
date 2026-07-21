using Application.DTOs.Customers;
using Application.Interfaces;
using Application.QueryParameters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Constants;
using SkyRoc.Authorization;

namespace SkyRoc.Controllers;

/// <summary>
///     公司管理控制器。
/// </summary>
[Route("api/companies")]
[Authorize]
[PermissionResource(PermissionCodes.Business.Customers.Resource)]
public class CompaniesController(ICompanyService service)
    : NamedCodeDataController<CompanyDto, CreateCompanyDto, UpdateCompanyDto, CompanyQueryParameters>(service);
