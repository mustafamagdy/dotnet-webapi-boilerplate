using Finbuckle.MultiTenant;
using FSH.WebApi.Application.Multitenancy;
using FSH.WebApi.Infrastructure.Persistence;
using FSH.WebApi.Shared.Authorization;
using FSH.WebApi.Shared.Multitenancy;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Serilog;

namespace FSH.WebApi.Infrastructure.Multitenancy;

internal static class Startup
{
  internal static IServiceCollection AddMultitenancy(this IServiceCollection services, IConfiguration config)
  {
    return services
      .AddDbContext<TenantDbContext>((p, m) =>
      {
        var databaseSettings = p.GetRequiredService<IOptions<DatabaseSettings>>().Value;
        m.UseDatabase(databaseSettings.DBProvider, databaseSettings.ConnectionString);
      })
      .AddMultiTenant<FSHTenantInfo>()
      .WithClaimStrategy(FSHClaims.Tenant)
      .WithHeaderStrategy(MultitenancyConstants.TenantIdName)
      .WithQueryStringStrategy(MultitenancyConstants.TenantIdName)
      .WithEFCoreStore<TenantDbContext, FSHTenantInfo>()
      .Services
      .AddScoped<ITenantService, TenantService>()
      .AddSingleton<ITenantConnectionStringBuilder, TenantConnectionStringBuilder>();
  }

  internal static IApplicationBuilder UseMultiTenancy(this IApplicationBuilder app) =>
    app.UseMultiTenant();

  private static FinbuckleMultiTenantBuilder<FSHTenantInfo> WithQueryStringStrategy(this FinbuckleMultiTenantBuilder<FSHTenantInfo> builder, string queryStringKey) =>
    builder.WithDelegateStrategy(context =>
    {
      if (context is not HttpContext httpContext)
      {
        return Task.FromResult((string?)null);
      }

      httpContext.Request.Query.TryGetValue(queryStringKey, out StringValues tenantIdParam);

      return Task.FromResult((string?)tenantIdParam.ToString());
    });
}