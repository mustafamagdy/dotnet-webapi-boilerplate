using FSH.WebApi.Domain.Common.Contracts;

namespace FSH.WebApi.Infrastructure.Multitenancy;

public class TenantSubscription : BaseEntity
{
  public string TenantId { get; set; }
  public DateTime ExpiryDate { get; set; }
  public bool IsDemo { get; set; }
}