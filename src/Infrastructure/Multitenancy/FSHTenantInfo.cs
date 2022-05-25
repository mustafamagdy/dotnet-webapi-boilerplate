using System.ComponentModel.DataAnnotations.Schema;
using Finbuckle.MultiTenant;
using FSH.WebApi.Shared.Multitenancy;
using MassTransit;

namespace FSH.WebApi.Infrastructure.Multitenancy;

public class TenantSubscriptionInfo
{
  public string Id { get; set; }
  public string TenantId { get; set; }
  public DateTime ExpiryDate { get; private set; }
  public bool IsDemo { get; private set; }

  public TenantSubscriptionInfo Renew(DateTime newExpiryDate)
  {
    ExpiryDate = newExpiryDate;
    return this;
  }

  public static TenantSubscriptionInfo CreateDemoForDays(string tenantId, int days)
  {
    return new TenantSubscriptionInfo
    {
      Id = NewId.Next().ToString(),
      TenantId = tenantId,
      ExpiryDate = DateTime.Now.AddDays(days),
      IsDemo = true
    };
  }

  public static TenantSubscriptionInfo CreateProdForDays(string tenantId, int days)
  {
    return new TenantSubscriptionInfo
    {
      Id = NewId.Next().ToString(),
      TenantId = tenantId,
      ExpiryDate = DateTime.Now.AddDays(days),
      IsDemo = false
    };
  }
}

public class FSHTenantInfo : ITenantInfo
{
  public FSHTenantInfo()
  {
  }

  public FSHTenantInfo(string id, string name, string? databaseName, string adminEmail, string? issuer = null)
  {
    Id = id;
    Identifier = id;
    Name = name;
    DatabaseName = databaseName ?? string.Empty;
    AdminEmail = adminEmail;
    IsActive = true;
    Issuer = issuer;
  }

  /// <summary>
  /// The actual TenantId, which is also used in the TenantId shadow property on the multitenant entities.
  /// </summary>
  public string Id { get; set; } = default!;

  /// <summary>
  /// The identifier that is used in headers/routes/querystrings. This is set to the same as Id to avoid confusion.
  /// </summary>
  public string Identifier { get; set; } = default!;

  public string Name { get; set; } = default!;
  public string DatabaseName { get; set; } = default!;

  public string AdminEmail { get; private set; } = default!;
  public bool IsActive { get; private set; }

  [NotMapped]
  public List<TenantSubscription> ActiveSubscriptions { get; private set; }

  /// <summary>
  /// Used by AzureAd Authorization to store the AzureAd Tenant Issuer to map against.
  /// </summary>
  public string? Issuer { get; set; }

  public string? Key => Name?.ToLower().Replace(" ", "-");

  public void Activate()
  {
    if (Id == MultitenancyConstants.Root.Id)
    {
      throw new InvalidOperationException("Invalid Tenant");
    }

    IsActive = true;
  }

  public void Deactivate()
  {
    if (Id == MultitenancyConstants.Root.Id)
    {
      throw new InvalidOperationException("Invalid Tenant");
    }

    IsActive = false;
  }

  public FSHTenantInfo SetActiveSubscriptions(List<TenantSubscription> activeSubscriptions)
  {
    ActiveSubscriptions = activeSubscriptions;
    return this;
  }

  string? ITenantInfo.Id { get => Id; set => Id = value ?? throw new InvalidOperationException("Id can't be null."); }

  string? ITenantInfo.Identifier { get => Identifier; set => Identifier = value ?? throw new InvalidOperationException("Identifier can't be null."); }

  string? ITenantInfo.Name { get => Name; set => Name = value ?? throw new InvalidOperationException("Name can't be null."); }

  string? ITenantInfo.DatabaseName { get => DatabaseName; set => DatabaseName = value ?? throw new InvalidOperationException
  ("Database Name can't be null."); }
}