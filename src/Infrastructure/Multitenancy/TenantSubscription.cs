namespace FSH.WebApi.Infrastructure.Multitenancy;

public class TenantSubscription
{
  public string Id { get; set; }
  public string TenantId { get; set; }
  public DateTime ExpiryDate { get; set; }
  public bool IsDemo { get; set; }
}