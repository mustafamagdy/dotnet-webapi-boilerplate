using FSH.WebApi.Application.Multitenancy;

namespace FSH.WebApi.Host.Controllers.Multitenancy;

public class TenantsController : VersionNeutralApiController
{
  [HttpGet]
  [MustHavePermission(FSHAction.View, FSHResource.Tenants)]
  [OpenApiOperation("Get a list of all tenants.", "")]
  public Task<List<TenantDto>> GetListAsync()
  {
    return Mediator.Send(new GetAllTenantsRequest());
  }

  [HttpGet("{id}")]
  [MustHavePermission(FSHAction.View, FSHResource.Tenants)]
  [OpenApiOperation("Get tenant details.", "")]
  public Task<TenantDto> GetAsync(string id)
  {
    return Mediator.Send(new GetTenantRequest(id));
  }

  [HttpPost]
  [MustHavePermission(FSHAction.Create, FSHResource.Tenants)]
  [OpenApiOperation("Create a new tenant.", "")]
  public Task<string> CreateAsync(CreateTenantRequest request)
  {
    return Mediator.Send(request);
  }

  [HttpPost("{id}/activate")]
  [MustHavePermission(FSHAction.Update, FSHResource.Tenants)]
  [OpenApiOperation("Activate a tenant.", "")]
  [ApiConventionMethod(typeof(FSHApiConventions), nameof(FSHApiConventions.Register))]
  public Task<string> ActivateAsync(string id)
  {
    return Mediator.Send(new ActivateTenantRequest(id));
  }

  [HttpPost("{id}/deactivate")]
  [MustHavePermission(FSHAction.Update, FSHResource.Tenants)]
  [OpenApiOperation("Deactivate a tenant.", "")]
  [ApiConventionMethod(typeof(FSHApiConventions), nameof(FSHApiConventions.Register))]
  public Task<string> DeactivateAsync(string id)
  {
    return Mediator.Send(new DeactivateTenantRequest(id));
  }

  [HttpGet("{tenantId}/subscriptions")]
  [MustHavePermission(FSHAction.View, FSHResource.Subscriptions)]
  [OpenApiOperation("Get a list of active subscriptions for a tenant.", "")]
  public Task<List<TenantSubscriptionDto>> GetActiveSubscriptions(string tenantId)
  {
    return Mediator.Send(new GetActiveSubscriptionsRequest(tenantId));
  }

  [HttpPost("{tenantId}/subscription/{subscriptionId}/renew")]
  [MustHavePermission(FSHAction.Update, FSHResource.Subscriptions)]
  [OpenApiOperation("Renew subscription for a tenant", "")]
  public async Task<ActionResult<string>> RenewSubscription(string tenantId, string subscriptionId, RenewSubscriptionRequest request)
  {
    return (tenantId != request.TenantId || subscriptionId != request.SubscriptionId)
      ? BadRequest()
      : Ok(await Mediator.Send(request));
  }
}