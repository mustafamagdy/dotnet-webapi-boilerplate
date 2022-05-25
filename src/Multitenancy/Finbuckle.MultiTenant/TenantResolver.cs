// Copyright Finbuckle LLC, Andrew White, and Contributors.
// Refer to the solution LICENSE file for more inforation.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Finbuckle.MultiTenant.Stores;
using Finbuckle.MultiTenant.Strategies;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Finbuckle.MultiTenant
{
  public class TenantIdResolver : ITenantIdResolver
  {
    private readonly IOptionsMonitor<MultiTenantOptions> options;
    private readonly ILoggerFactory? loggerFactory;

    public TenantIdResolver(IEnumerable<IMultiTenantStrategy> strategies, IOptionsMonitor<MultiTenantOptions> options)
      : this(strategies, options, loggerFactory: null)
    {
    }

    public TenantIdResolver(IEnumerable<IMultiTenantStrategy> strategies, IOptionsMonitor<MultiTenantOptions> options, ILoggerFactory? loggerFactory)
    {
      this.options = options;
      this.loggerFactory = loggerFactory;

      Strategies = strategies.OrderByDescending(s => s.Priority);
    }

    public IEnumerable<IMultiTenantStrategy> Strategies { get; set; }

    public async Task<string> ResolveAsync(object context)
    {
      string? identifier = null;
      foreach (var strategy in Strategies)
      {
        var strategyWrapper = new MultiTenantStrategyWrapper(strategy, loggerFactory?.CreateLogger(strategy.GetType()) ?? NullLogger.Instance);
        identifier = await strategyWrapper.GetIdentifierAsync(context);

        if (identifier != null)
          break;

        await options.CurrentValue.Events.OnTenantNotResolved(new TenantNotResolvedContext { Context = context, Identifier = identifier });
      }

      return identifier!;
    }
  }

  public class TenantResolver<T> : ITenantResolver<T>, ITenantResolver
    where T : class, ITenantInfo, new()
  {
    private readonly ITenantIdResolver _tenantIdResolver;
    private readonly IOptionsMonitor<MultiTenantOptions> _options;
    private readonly ILoggerFactory? _loggerFactory;

    public TenantResolver(IEnumerable<IMultiTenantStrategy> strategies, IEnumerable<IMultiTenantStore<T>> stores,
      ITenantIdResolver tenantIdResolver, IOptionsMonitor<MultiTenantOptions> options) :
      this(strategies, stores, tenantIdResolver, options, null)
    {
    }

    public TenantResolver(IEnumerable<IMultiTenantStrategy> strategies, IEnumerable<IMultiTenantStore<T>> stores,
      ITenantIdResolver tenantIdResolver, IOptionsMonitor<MultiTenantOptions> options, ILoggerFactory? loggerFactory)
    {
      Stores = stores;
      _tenantIdResolver = tenantIdResolver;
      this._options = options;
      this._loggerFactory = loggerFactory;

      Strategies = strategies.OrderByDescending(s => s.Priority);
    }

    public IEnumerable<IMultiTenantStrategy> Strategies { get; set; }
    public IEnumerable<IMultiTenantStore<T>> Stores { get; set; }

    public async Task<IMultiTenantContext<T>?> ResolveAsync(object context)
    {
      IMultiTenantContext<T>? result = null;

      foreach (var strategy in Strategies)
      {
        string? identifier = await _tenantIdResolver.ResolveAsync(context);

        if (_options.CurrentValue.IgnoredIdentifiers.Contains(identifier, StringComparer.OrdinalIgnoreCase))
        {
          (_loggerFactory?.CreateLogger(GetType()) ?? NullLogger.Instance).LogInformation("Ignored identifier: {Identifier}", identifier);
          identifier = null;
        }

        if (identifier != null)
        {
          foreach (var store in Stores)
          {
            var storeWrapper = new MultiTenantStoreWrapper<T>(store, _loggerFactory?.CreateLogger(store.GetType()) ?? NullLogger.Instance);
            var tenantInfo = await storeWrapper.TryGetByIdentifierAsync(identifier);
            if (tenantInfo != null)
            {
              result = new MultiTenantContext<T>();
              result.StoreInfo = new StoreInfo<T> { Store = store, StoreType = store.GetType() };
              result.StrategyInfo = new StrategyInfo { Strategy = strategy, StrategyType = strategy.GetType() };
              result.TenantInfo = tenantInfo;

              await _options.CurrentValue.Events.OnTenantResolved(new TenantResolvedContext
              {
                Context =
                  context,
                TenantInfo = tenantInfo, StrategyType = strategy.GetType(), StoreType = store.GetType()
              });

              break;
            }
          }

          if (result != null)
            break;

          await _options.CurrentValue.Events.OnTenantNotResolved(new TenantNotResolvedContext { Context = context, Identifier = identifier });
        }
      }

      return result;
    }

    async Task<object?> ITenantResolver.ResolveAsync(object context)
    {
      var multiTenantContext = await ResolveAsync(context);
      return multiTenantContext;
    }
  }
}