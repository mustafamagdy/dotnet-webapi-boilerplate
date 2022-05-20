﻿using Finbuckle.MultiTenant;
using FSH.WebApi.Application.Common.Exceptions;
using FSH.WebApi.Application.Common.Interfaces;
using FSH.WebApi.Application.Common.Mailing;
using FSH.WebApi.Application.Common.Persistence;
using FSH.WebApi.Application.Multitenancy;
using FSH.WebApi.Infrastructure.Persistence;
using FSH.WebApi.Infrastructure.Persistence.Context;
using FSH.WebApi.Infrastructure.Persistence.Initialization;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace FSH.WebApi.Infrastructure.Multitenancy;

internal class TenantService : ITenantService {
  private readonly IMultiTenantStore<FSHTenantInfo> _tenantStore;
  private readonly ApplicationDbContext _dbContext;
  private readonly IConnectionStringSecurer _csSecurer;
  private readonly IDatabaseInitializer _dbInitializer;
  private readonly IJobService _jobService;
  private readonly IMailService _mailService;
  private readonly IEmailTemplateService _templateService;
  private readonly IStringLocalizer _t;
  private readonly DatabaseSettings _dbSettings;

  public TenantService(
    IMultiTenantStore<FSHTenantInfo> tenantStore,
    ApplicationDbContext dbContext,
    IConnectionStringSecurer csSecurer,
    IDatabaseInitializer dbInitializer,
    IJobService jobService,
    IMailService mailService,
    IEmailTemplateService templateService,
    IStringLocalizer<TenantService> localizer,
    IOptions<DatabaseSettings> dbSettings) {
    _tenantStore = tenantStore;
    _dbContext = dbContext;
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
    var subscription = TryCreateSubscription(tenant);
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

  private TenantSubscriptionInfo TryCreateSubscription(FSHTenantInfo tenant) { throw new NotImplementedException(); }

  public async Task<string> ActivateAsync(string id) {
    var tenant = await GetTenantInfoAsync(id);

    if (tenant.IsActive) {
      throw new ConflictException(_t["Tenant is already Activated."]);
    }

    tenant.Activate();

    await _tenantStore.TryUpdateAsync(tenant);

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

  public async Task<string> UpdateSubscription(string id, DateTime extendedExpiryDate) {
    var tenant = await GetTenantInfoAsync(id);

    tenant.SetValidity(extendedExpiryDate);

    await _tenantStore.TryUpdateAsync(tenant);

    return _t[$"Tenant {0}'s Subscription Upgraded. Now Valid till {1}.", id, tenant.ValidUpto];
  }

  public async Task<IEnumerable<TenantSubscriptionDto>> GetActiveSubscriptions(string tenantId) {
    var subscription = (await _dbContext.TenantSubscriptions
      .Where(a => a.TenantId == tenantId && a.ExpiryDate > DateTime.Now)
      .ToListAsync()).Adapt<List<TenantSubscriptionDto>>();

    return subscription;
  }

  private async Task<FSHTenantInfo> GetTenantInfoAsync(string id) =>
    await _tenantStore.TryGetAsync(id)
    ?? throw new NotFoundException(_t["{0} {1} Not Found.", typeof(FSHTenantInfo).Name, id]);
}