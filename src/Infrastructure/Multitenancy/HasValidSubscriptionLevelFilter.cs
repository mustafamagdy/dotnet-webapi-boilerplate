using Finbuckle.MultiTenant;
using FSH.WebApi.Application.Multitenancy;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FSH.WebApi.Infrastructure.Multitenancy
{
  public class HasValidSubscriptionLevelFilter : IAsyncActionFilter
  {
    private readonly ITenantResolver _tenantResolver;
    private readonly ITenantService _tenantService;

    public HasValidSubscriptionLevelFilter(ITenantResolver tenantResolver, ITenantService tenantService)
    {
      _tenantResolver = tenantResolver;
      _tenantService = tenantService;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
      var descriptor = context.ActionDescriptor as ControllerActionDescriptor;
      if (descriptor?.MethodInfo
            .GetCustomAttributes(typeof(HasValidSubscriptionLevelAttribute), true)
            .ToList()
            .FirstOrDefault() is HasValidSubscriptionLevelAttribute subscriptionLevel)
      {
        if (await _tenantResolver.ResolveAsync(context.HttpContext) is MultiTenantContext<FSHTenantInfo> tenantContext)
        {
          var tenant = await _tenantService.GetByIdAsync(tenantContext.TenantInfo?.Id);
          if (tenant.ActiveSubscriptions?.Count == 0 && subscriptionLevel.Level != SubscriptionLevel.Basic)
          {
            throw new FeatureNotAllowedException();
          }
        }

        await next();
      }

      await next();
    }
  }

  [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
  public class HasValidSubscriptionLevelAttribute : Attribute
  {
    public SubscriptionLevel Level { get; }

    public HasValidSubscriptionLevelAttribute(SubscriptionLevel level)
    {
      Level = level;
    }
  }

  public enum SubscriptionLevel
  {
    Basic,
    Full
  }
}