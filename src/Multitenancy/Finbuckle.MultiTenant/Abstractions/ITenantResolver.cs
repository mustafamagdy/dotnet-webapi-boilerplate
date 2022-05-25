// Copyright Finbuckle LLC, Andrew White, and Contributors.
// Refer to the solution LICENSE file for more inforation.

using System.Collections.Generic;
using System.Threading.Tasks;
using Finbuckle.MultiTenant.Strategies;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Finbuckle.MultiTenant
{
  public interface ITenantResolver
  {
    Task<object?> ResolveAsync(object context);
  }

  public interface ITenantResolver<T>
    where T : class, ITenantInfo, new()
  {
    IEnumerable<IMultiTenantStrategy> Strategies { get; }
    IEnumerable<IMultiTenantStore<T>> Stores { get; }

    Task<IMultiTenantContext<T>?> ResolveAsync(object context);
  }


  public interface ITenantIdResolver
  {
    Task<string> ResolveAsync(object context);
  }
}