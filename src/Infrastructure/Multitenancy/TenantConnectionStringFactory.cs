using System.Text.Json;
using FSH.WebApi.Application.Common.Persistence;
using FSH.WebApi.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace FSH.WebApi.Infrastructure.Multitenancy
{
  public interface ITenantConnectionStringFactory
  {
    string TryGetTenantConnectionString(string tenantKey, string environment);
    bool SaveConnectionString(string tenantKey, string environment, DatabaseSettings dbSetting);
    void RemoveConnectionStringsForTenant(string tenantKey);
  }

  public class TenantConnectionStringFactory : ITenantConnectionStringFactory
  {
    private static object _lock = new();
    private readonly IConfiguration _config;
    private readonly IConnectionStringValidator _csValidator;
    private readonly IHostEnvironment _env;
    private readonly IHttpContextAccessor _httpContextAccessor;


    public TenantConnectionStringFactory(IConfiguration config, IConnectionStringValidator csValidator, IHostEnvironment env, IHttpContextAccessor httpContextAccessor)
    {
      _config = config;
      _csValidator = csValidator;
      _env = env;
      _httpContextAccessor = httpContextAccessor;
    }

    public string TryGetTenantConnectionString(string tenantKey, string environment)
    {
      string? tenantConnectionString = _config.GetSection($"Tenants:{tenantKey}").Value;
      return tenantConnectionString;
    }

    public bool SaveConnectionString(string tenantKey, string environment, DatabaseSettings dbSetting)
    {
      if (!_csValidator.TryValidate(dbSetting.ConnectionString, dbSetting.DBProvider))
      {
        throw new ArgumentException($"Invalid connection string for tenant {tenantKey}", nameof(dbSetting.ConnectionString));
      }

      lock (_lock)
      {
        const string configurationsDirectory = "Configurations";
        string configFile = $"{configurationsDirectory}/tenant-dbs.json";
        string fileFullPath = Path.Combine(_env.ContentRootPath, configFile);
        if (!File.Exists(fileFullPath))
        {
          throw new FileNotFoundException("Tenant database connection config file not found");
        }

        using var file = File.Open(fileFullPath, FileMode.Open, FileAccess.ReadWrite);
        using var sr = new StreamReader(file);
        string tenants = sr.ReadToEnd();
        sr.Close();
        var settings = JsonSerializer.Deserialize<TenantsDatabases>(tenants);
        var tenantSetting = settings.Tenants
          .FirstOrDefault(a =>
            string.Equals(a.TenantKey, tenantKey, StringComparison.CurrentCultureIgnoreCase) &&
            string.Equals(a.Environment, environment, StringComparison.CurrentCultureIgnoreCase));

        if (tenantSetting == null)
        {
          tenantSetting = new TenantSetting();
          tenantSetting.TenantKey = tenantKey;
          tenantSetting.Environment = environment;
          settings.Tenants.Add(tenantSetting);
        }

        tenantSetting.ConnectionString = dbSetting.ConnectionString;
        tenantSetting.DBProvider = dbSetting.DBProvider;

        using var sw = new StreamWriter(fileFullPath, append: false);
        string serializedSettings = JsonSerializer.Serialize(settings, new JsonSerializerOptions
        {
          WriteIndented = true
        });

        sw.WriteAsync(serializedSettings);
        return true;
      }
    }

    public void RemoveConnectionStringsForTenant(string tenantKey)
    {
      lock (_lock)
      {
        const string configurationsDirectory = "Configurations";
        string configFile = $"{configurationsDirectory}/tenant-dbs.json";
        string fileFullPath = Path.Combine(_env.ContentRootPath, configFile);
        if (!File.Exists(fileFullPath))
        {
          throw new FileNotFoundException("Tenant database connection config file not found");
        }

        using var file = File.Open(fileFullPath, FileMode.Open, FileAccess.ReadWrite);
        using var sr = new StreamReader(file);
        string tenants = sr.ReadToEnd();
        sr.Close();
        var settings = JsonSerializer.Deserialize<TenantsDatabases>(tenants);
        var tenantSetting = settings.Tenants.Where(a => !string.Equals(a.TenantKey, tenantKey, StringComparison.CurrentCultureIgnoreCase));
        settings.Tenants = tenantSetting.ToList();

        using var sw = new StreamWriter(fileFullPath, append: false);
        string serializedSettings = JsonSerializer.Serialize(settings, new JsonSerializerOptions
        {
          WriteIndented = true
        });

        sw.WriteAsync(serializedSettings);
      }
    }
  }

  internal class TenantsDatabases
  {
    public List<TenantSetting> Tenants { get; set; }
  }

  internal class TenantSetting
  {
    public string TenantKey { get; set; }
    public string Environment { get; set; }
    public string DBProvider { get; set; }
    public string ConnectionString { get; set; }
  }
}