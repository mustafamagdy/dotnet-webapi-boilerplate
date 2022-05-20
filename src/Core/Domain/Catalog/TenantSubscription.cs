namespace FSH.WebApi.Domain.Catalog;

public class TenantSubscription : AuditableEntity {
  public string Id { get; set; }
  public string TenantId { get; set; }
  public DateTime ExpiryDate { get; set; }
  public bool IsDemo { get; set; }
}