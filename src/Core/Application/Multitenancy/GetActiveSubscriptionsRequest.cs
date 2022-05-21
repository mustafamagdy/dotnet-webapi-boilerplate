namespace FSH.WebApi.Application.Multitenancy;

public class GetActiveSubscriptionsRequest : IRequest<List<TenantSubscriptionDto>>
{
  public string TenantId { get; set; } = default!;

  public GetActiveSubscriptionsRequest(string tenantId) => TenantId = tenantId;
}

public class
  GetActiveSubscriptionsRequestHandler : IRequestHandler<GetActiveSubscriptionsRequest, List<TenantSubscriptionDto>>
{
  private readonly ITenantService _tenantService;

  public GetActiveSubscriptionsRequestHandler(ITenantService tenantService) => _tenantService = tenantService;

  public async Task<List<TenantSubscriptionDto>> Handle(GetActiveSubscriptionsRequest request, CancellationToken
    cancellationToken) =>
    (await _tenantService.GetActiveSubscriptions(request.TenantId)).ToList();
}