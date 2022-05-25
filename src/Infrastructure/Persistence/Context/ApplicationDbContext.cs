using Finbuckle.MultiTenant;
using FSH.WebApi.Application.Common.Events;
using FSH.WebApi.Application.Common.Interfaces;
using FSH.WebApi.Domain.Catalog;
using FSH.WebApi.Infrastructure.Multitenancy;
using FSH.WebApi.Infrastructure.Persistence.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FSH.WebApi.Infrastructure.Persistence.Context;

public class ApplicationDbContext : BaseDbContext
{
  public ApplicationDbContext(ITenantInfo currentTenant, DbContextOptions options, ICurrentUser currentUser,
    ISerializerService serializer, ITenantConnectionStringBuilder csBuilder, IOptions<DatabaseSettings> dbSettings,
    IEventPublisher events)
    : base(currentTenant, options, currentUser, serializer, csBuilder, dbSettings, events)
  {
  }

  public DbSet<Product> Products => Set<Product>();
  public DbSet<Brand> Brands => Set<Brand>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);

    modelBuilder.HasDefaultSchema(SchemaNames.Catalog);
  }
}