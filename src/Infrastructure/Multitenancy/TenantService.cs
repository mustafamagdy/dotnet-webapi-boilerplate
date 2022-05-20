using Finbuckle.MultiTenant;
using FSH.WebApi.Application.Common.Exceptions;
using FSH.WebApi.Application.Common.Interfaces;
using FSH.WebApi.Application.Common.Mailing;
using FSH.WebApi.Application.Common.Persistence;
using FSH.WebApi.Application.Multitenancy;
using FSH.WebApi.Infrastructure.Persistence;
using FSH.WebApi.Infrastructure.Persistence.Context;
using FSH.WebApi.Infrastructure.Persistence.Initialization;
using Mapster;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace FSH.WebApi.Infrastructure.Multitenancy;

internal class TenantService : ITenantService {
  private readonly IMultiTenantStore<FSHTenantInfo> _tenantStore;
  private readonly TenantDbContext _tenantDbContext;
  private readonly IConnectionStringSecurer _csSecurer;
  private readonly IDatabaseInitializer _dbInitializer;
  private readonly IJobService _jobService;
  private readonly IMailService _mailService;
  private readonly IEmailTemplateService _templateService;
  private readonly IStringLocalizer _t;
  private readonly DatabaseSettings _dbSettings;

  public TenantService(
    IMultiTenantStore<FSHTenantInfo> tenantStore,
    TenantDbContext tenantDbContext,
    IConnectionStringSecurer csSecurer,
    IDatabaseInitializer dbInitializer,
    IJobService jobService,
    IMailService mailService,
    IEmailTemplateService templateService,
    IStringLocalizer<TenantService> localizer,
    IOptions<DatabaseSettings> dbSettings) {
    _tenantStore = tenantStore;
    _tenantDbContext = tenantDbContext;
    _csSecurer = csSecurer;
    _dbInitializer = dbInitializer;
    _jobService = jobService;
    _mailService = mailService;
    _templateService = templateService;
    _t = localizer;
    _dbSettings = dbSettings.Value;
  }

  public async Task<List<TenantDto>> GetAllAsync() {
    var tenants = (await _tenantStore.GetAllAsync()).Adapt<List<TenantDto>>();
    tenants.ForEach(t => t.ConnectionString = _csSecurer.MakeSecure(t.ConnectionString));
    return tenants;
  }

  public async Task<bool> ExistsWithIdAsync(string id) =>
    await _tenantStore.TryGetAsync(id) is not null;

  public async Task<bool> ExistsWithNameAsync(string name) =>
    (await _tenantStore.GetAllAsync()).Any(t => t.Name == name);

  public async Task<TenantDto> GetByIdAsync(string id) =>
    (await GetTenantInfoAsync(id))
    .Adapt<TenantDto>();

  public async Task<string> CreateAsync(CreateTenantRequest request, CancellationToken cancellationToken) {
    if (request.ConnectionString?.Trim() == _dbSettings.ConnectionString.Trim())
      request.ConnectionString = string.Empty;

    var tenant = new FSHTenantInfo(request.Id, request.Name, request.ConnectionString, request.AdminEmail,
      request.Issuer);

    await _tenantStore.TryAddAsync(tenant);
    var subscription = await TryCreateSubscription(tenant);
    //todo: move urls to settings
    var demoUrl = $"https://demo.abcd.com/{tenant.Key}";
    var prodUrl = $"https://prod.abcd.com/{tenant.Key}";
    try {
      await _dbInitializer.InitializeApplicationDbForTenantAsync(tenant, cancellationToken);

      var eMailModel = new TenantCreatedEmailModel() {
        AdminEmail = request.AdminEmail,
        TenantName = request.Name,
        SubscriptionExpiryDate = subscription.ExpiryDate,
        SiteUrl = subscription.IsDemo ? demoUrl : prodUrl
      };

      var mailRequest = new MailRequest(
        new List<string> { request.AdminEmail },
        _t["Subscription Created"],
        _templateService.GenerateEmailTemplate("email-subscription", eMailModel));

      _jobService.Enqueue(() => _mailService.SendAsync(mailRequest, CancellationToken.None));
    } catch {
      await _tenantStore.TryRemoveAsync(request.Id);
      throw;
    }

    return tenant.Id;
  }

  private async Task<TenantSubscriptionInfo> TryCreateSubscription(FSHTenantInfo tenant) {
    var newExpiryDate = DateTime.Now.AddMonths(1);
    var subscription = new TenantSubscription {
      Id = NewId.Next().ToString(),
      TenantId = tenant.Id,
      ExpiryDate = newExpiryDate,
      IsDemo = false
    };

    await _tenantDbContext.AddAsync(subscription);

    return subscription.Adapt<TenantSubscriptionInfo>();
  }

  public async Task<string> ActivateAsync(string id) {
    var tenant = await GetTenantInfoAsync(id);

    if (tenant.IsActive) {
      throw new ConflictException(_t["Tenant is already Activated."]);
    }

    tenant.Activate();

    await _tenantStore.TryUpdateAsync(tenant);

    var subscriptions = await _tenantDbContext
      .Set<TenantSubscription>()
      .Where(a => a.TenantId == tenant.Id && a.IsDemo == false)
      .OrderByDescending(a => a.ExpiryDate)
      .ToArrayAsync();

    var newExpiryDate = DateTime.Now.AddMonths(1);
    if (subscriptions.Any()) {
      var activeSubscription = subscriptions.First();
      activeSubscription.ExpiryDate = newExpiryDate;
    } else {
      await TryCreateSubscription(tenant);
    }

    return _t["Tenant {0} is now Activated.", id];
  }

  public async Task<string> DeactivateAsync(string id) {
    var tenant = await GetTenantInfoAsync(id);

    if (!tenant.IsActive) {
      throw new ConflictException(_t["Tenant is already Deactivated."]);
    }

    tenant.Deactivate();

    await _tenantStore.TryUpdateAsync(tenant);

    return _t[$"Tenant {0} is now Deactivated.", id];
  }

  public async Task<string> RenewSubscription(string subscriptionId, DateTime extendedExpiryDate) {
    var subscription = await _tenantDbContext.Set<TenantSubscription>().FindAsync(subscriptionId);
    if (subscription == null) {
      throw new NotFoundException(_t["Subscription not found."]);
    }

    subscription.ExpiryDate = extendedExpiryDate;
    _tenantDbContext.Update(subscription);

    return _t[$"Subscription {0} renewed. Now Valid till {1}.", subscription.Id, subscription.ExpiryDate];
  }

  public async Task<IEnumerable<TenantSubscriptionDto>> GetActiveSubscriptions(string tenantId) {
    var subscription = (await _tenantDbContext.Set<TenantSubscription>()
      .Where(a => a.TenantId == tenantId && a.ExpiryDate > DateTime.Now)
      .ToListAsync()).Adapt<List<TenantSubscriptionDto>>();

    return subscription;
  }

  private async Task<FSHTenantInfo> GetTenantInfoAsync(string id) =>
    await _tenantStore.TryGetAsync(id)
    ?? throw new NotFoundException(_t["{0} {1} Not Found.", typeof(FSHTenantInfo).Name, id]);
}