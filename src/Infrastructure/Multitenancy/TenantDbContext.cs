using Finbuckle.MultiTenant.Stores;
using FSH.WebApi.Infrastructure.Persistence.Configuration;
using Microsoft.EntityFrameworkCore;

namespace FSH.WebApi.Infrastructure.Multitenancy;

public class TenantDbContext : EFCoreStoreDbContext<FSHTenantInfo>
{
  public TenantDbContext(DbContextOptions<TenantDbContext> options)
    : base(options)
  {
  }

  public DbSet<TenantSubscription> Subscriptions => Set<TenantSubscription>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);

    modelBuilder.Entity<FSHTenantInfo>().ToTable("Tenants", SchemaNames.MultiTenancy);
    // modelBuilder.Entity<TenantSubscription>().ToTable("Subscriptions", SchemaNames.MultiTenancy);
  }
}