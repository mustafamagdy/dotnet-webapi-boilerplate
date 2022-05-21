namespace FSH.WebApi.Application.Multitenancy;

public class TenantSubscriptionDto
{
  public string Id { get; set; }
  public string TenantId { get; set; }
  public DateTime ExpiryDate { get; set; }
  public bool IsDemo { get; set; }
}