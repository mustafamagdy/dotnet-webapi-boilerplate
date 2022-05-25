namespace FSH.WebApi.Application.Multitenancy;

public class TenantDto
{
  public string Id { get; set; } = default!;
  public string Name { get; set; } = default!;
  public string? DatabaseName { get; set; }
  public string AdminEmail { get; set; } = default!;
  public bool IsActive { get; set; }
  public string? Issuer { get; set; }

  public List<TenantSubscriptionDto> ActiveSubscriptions { get; set; }
}