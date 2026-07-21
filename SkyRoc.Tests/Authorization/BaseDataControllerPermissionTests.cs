using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Routing;
using Shared.Constants;
using SkyRoc.Authorization;
using SkyRoc.Controllers;
using Xunit;

namespace SkyRoc.Tests.Authorization;

public class BaseDataControllerPermissionTests
{
    private static readonly IReadOnlyDictionary<Type, string> ExpectedResources =
        new Dictionary<Type, string>
        {
            [typeof(GoodsController)] = PermissionCodes.Business.Goods.Resource,
            [typeof(GoodsTypesController)] = PermissionCodes.Business.Goods.Resource,
            [typeof(GoodsUnitsController)] = PermissionCodes.Business.Goods.Resource,
            [typeof(CompaniesController)] = PermissionCodes.Business.Customers.Resource,
            [typeof(CustomersController)] = PermissionCodes.Business.Customers.Resource,
            [typeof(CustomerTagsController)] = PermissionCodes.Business.Customers.Resource,
            [typeof(CustomerSubAccountsController)] = PermissionCodes.Business.Customers.Resource,
            [typeof(QuotationsController)] = PermissionCodes.Business.Pricing.Resource,
            [typeof(QuotationGoodsController)] = PermissionCodes.Business.Pricing.Resource,
            [typeof(CustomerProtocolsController)] = PermissionCodes.Business.Pricing.Resource,
            [typeof(CustomerProtocolGoodsController)] = PermissionCodes.Business.Pricing.Resource,
            [typeof(SuppliersController)] = PermissionCodes.Business.Purchases.Resource,
            [typeof(PurchasersController)] = PermissionCodes.Business.Purchases.Resource,
            [typeof(PurchaseRulesController)] = PermissionCodes.Business.Purchases.Resource,
            [typeof(WaresController)] = PermissionCodes.Business.Storage.Resource,
            [typeof(CarriersController)] = PermissionCodes.Business.Delivery.Resource,
            [typeof(DriversController)] = PermissionCodes.Business.Delivery.Resource,
            [typeof(RoutesController)] = PermissionCodes.Business.Delivery.Resource
        };

    private static readonly IReadOnlyDictionary<string, string> ExpectedCrudActions =
        new Dictionary<string, string>
        {
            [nameof(BaseDataController<object, object, TestUpdateDto, TestQuery>.GetPaged)] = PermissionActions.Read,
            [nameof(BaseDataController<object, object, TestUpdateDto, TestQuery>.GetAll)] = PermissionActions.Read,
            [nameof(BaseDataController<object, object, TestUpdateDto, TestQuery>.GetById)] = PermissionActions.Read,
            [nameof(BaseDataController<object, object, TestUpdateDto, TestQuery>.Create)] = PermissionActions.Create,
            [nameof(BaseDataController<object, object, TestUpdateDto, TestQuery>.Update)] = PermissionActions.Update,
            [nameof(BaseDataController<object, object, TestUpdateDto, TestQuery>.Delete)] = PermissionActions.Delete,
            [nameof(BaseDataController<object, object, TestUpdateDto, TestQuery>.BatchDelete)] = PermissionActions.Delete,
            [nameof(BaseDataController<object, object, TestUpdateDto, TestQuery>.ToggleStatus)] = PermissionActions.Update
        };

    private static readonly IReadOnlyDictionary<Type, IReadOnlyDictionary<string, string>> ExpectedSpecialPolicies =
        new Dictionary<Type, IReadOnlyDictionary<string, string>>
        {
            [typeof(GoodsController)] = new Dictionary<string, string>
            {
                [nameof(GoodsController.ToggleSaleStatus)] = PermissionCodes.Business.Goods.Update
            },
            [typeof(GoodsTypesController)] = new Dictionary<string, string>
            {
                [nameof(GoodsTypesController.GetTree)] = PermissionCodes.Business.Goods.Read
            },
            [typeof(GoodsUnitsController)] = new Dictionary<string, string>
            {
                [nameof(GoodsUnitsController.GetByGoodsId)] = PermissionCodes.Business.Goods.Read
            },
            [typeof(CustomerTagsController)] = new Dictionary<string, string>
            {
                [nameof(CustomerTagsController.GetTree)] = PermissionCodes.Business.Customers.Read
            },
            [typeof(QuotationsController)] = new Dictionary<string, string>
            {
                [nameof(QuotationsController.GetLegacyOptions)] = PermissionCodes.Business.Pricing.Read,
                [nameof(QuotationsController.ToggleAudit)] = PermissionCodes.Business.Pricing.Audit
            },
            [typeof(CustomerProtocolsController)] = new Dictionary<string, string>
            {
                [nameof(CustomerProtocolsController.GetLegacyOptions)] = PermissionCodes.Business.Pricing.Read
            },
            [typeof(RoutesController)] = new Dictionary<string, string>
            {
                [nameof(RoutesController.DispatchCustomers)] = PermissionCodes.Business.Delivery.Update
            }
        };

    [Fact]
    public void BaseDataControllers_DeclareExpectedPermissionResources()
    {
        var controllerTypes = typeof(BaseDataController<,,,>).Assembly.GetTypes()
            .Where(type => !type.IsAbstract && InheritsFromBaseDataController(type))
            .ToList();

        Assert.Equal(ExpectedResources.Keys.OrderBy(type => type.Name), controllerTypes.OrderBy(type => type.Name));
        foreach (var (controllerType, expectedResource) in ExpectedResources)
        {
            var resource = Assert.Single(
                controllerType.GetCustomAttributes<PermissionResourceAttribute>(true));
            Assert.Equal(expectedResource, resource.Resource);
        }
    }

    [Fact]
    public void BaseDataCrudActions_RequireExpectedResourceOperations()
    {
        var actions = typeof(BaseDataController<,,,>).GetMethods(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

        Assert.Equal(ExpectedCrudActions.Count, actions.Length);
        foreach (var action in actions)
        {
            Assert.True(
                ExpectedCrudActions.TryGetValue(action.Name, out var expectedPermissionAction),
                $"{action.Name} is missing from the permission regression test.");

            var requirement = Assert.Single(action.GetCustomAttributes<ResourcePermissionAttribute>());
            Assert.Equal(expectedPermissionAction, requirement.Action);
        }
    }

    [Fact]
    public void NamedCodeOptionActions_RequireReadPermission()
    {
        var actions = typeof(NamedCodeDataController<,,,>).GetMethods(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(action => action.GetCustomAttributes<HttpMethodAttribute>().Any())
            .ToList();

        Assert.Equal(3, actions.Count);
        Assert.All(actions, action =>
            Assert.Equal(
                PermissionActions.Read,
                Assert.Single(action.GetCustomAttributes<ResourcePermissionAttribute>()).Action));
    }

    [Fact]
    public void BaseDataSpecialActions_RequireExpectedPermissionPolicies()
    {
        foreach (var controllerType in ExpectedResources.Keys)
        {
            var actions = controllerType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(action => action.GetCustomAttributes<HttpMethodAttribute>().Any())
                .ToList();
            var expectedPolicies = ExpectedSpecialPolicies.GetValueOrDefault(
                controllerType,
                new Dictionary<string, string>());

            Assert.Equal(expectedPolicies.Count, actions.Count);
            foreach (var action in actions)
            {
                Assert.True(
                    expectedPolicies.TryGetValue(action.Name, out var expectedPolicy),
                    $"{controllerType.Name}.{action.Name} is missing from the permission regression test.");

                var policy = Assert.Single(
                    action.GetCustomAttributes<AuthorizeAttribute>(),
                    attribute => !string.IsNullOrWhiteSpace(attribute.Policy)).Policy;
                Assert.Equal(expectedPolicy, policy);
                Assert.Empty(action.GetCustomAttributes<AllowAnonymousAttribute>());
            }
        }
    }

    private static bool InheritsFromBaseDataController(Type type)
    {
        for (var current = type.BaseType; current is not null; current = current.BaseType)
            if (current.IsGenericType
                && current.GetGenericTypeDefinition() == typeof(BaseDataController<,,,>))
                return true;

        return false;
    }

    private sealed class TestUpdateDto : Application.DTOs.IHasId
    {
        public Guid Id { get; set; }
    }

    private sealed class TestQuery : Application.QueryParameters.PagedQueryParameters;
}
