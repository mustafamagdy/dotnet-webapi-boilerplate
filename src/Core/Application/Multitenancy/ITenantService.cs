namespace FSH.WebApi.Application.Multitenancy;

public interface ITenantService
{
    Task<List<TenantDto>> GetAllAsync();
    Task<bool> ExistsWithIdAsync(string id);
    Task<bool> ExistsWithNameAsync(string name);
    Task<TenantDto> GetByIdAsync(string id);
    Task<string> CreateAsync(CreateTenantRequest request, CancellationToken cancellationToken);
    Task<string> ActivateAsync(string id);
    Task<string> DeactivateAsync(string id);
    Task<string> RenewSubscription(string subscriptionId, DateTime extendedExpiryDate);
    Task<IEnumerable<TenantSubscriptionDto>> GetActiveSubscriptions(string tenantId);
    Task<bool> DatabaseExistAsync(string databaseName);
}