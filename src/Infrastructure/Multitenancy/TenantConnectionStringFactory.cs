using System.Text.Json;
using FSH.WebApi.Application.Common.Persistence;
using FSH.WebApi.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace FSH.WebApi.Infrastructure.Multitenancy
{
  public interface ITenantConnectionStringBuilder
  {
    string BuildConnectionString(string databaseName);
  }

  public class TenantConnectionStringBuilder : ITenantConnectionStringBuilder
  {
    private readonly IConfiguration _config;
    private readonly IConnectionStringValidator _csValidator;
    private readonly IHostEnvironment _env;

    public TenantConnectionStringBuilder(IConfiguration config, IConnectionStringValidator csValidator, IHostEnvironment env)
    {
      _config = config;
      _csValidator = csValidator;
      _env = env;
    }

    public string BuildConnectionString(string databaseName)
    {
      string connectionStringTemplate = _config["DatabaseSettings:ConnectionStringTemplate"];
      string providerName = _config["DatabaseSettings:DBProvider"];

      string dbName = $"{_env.GetShortenName()}-{databaseName}";
      string connectionString = string.Format(connectionStringTemplate, dbName);
      if (!_csValidator.TryValidate(connectionString, providerName))
        throw new ArgumentException(connectionString);

      return connectionString;
    }
  }
}