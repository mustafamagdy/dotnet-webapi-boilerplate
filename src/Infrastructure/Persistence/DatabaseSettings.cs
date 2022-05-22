using System.ComponentModel.DataAnnotations;

namespace FSH.WebApi.Infrastructure.Persistence;

public class DatabaseSettings : IValidatableObject
{
  public DatabaseSettings()
  {
  }

  public DatabaseSettings(string dbProvider, string connectionString)
  {
    DBProvider = dbProvider;
    ConnectionString = connectionString;
  }

  public string DBProvider { get; init; } = string.Empty;
  public string ConnectionString { get; init; } = string.Empty;

  public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
  {
    if (string.IsNullOrEmpty(DBProvider))
    {
      yield return new ValidationResult(
        $"{nameof(DatabaseSettings)}.{nameof(DBProvider)} is not configured",
        new[] { nameof(DBProvider) });
    }

    if (string.IsNullOrEmpty(ConnectionString))
    {
      yield return new ValidationResult(
        $"{nameof(DatabaseSettings)}.{nameof(ConnectionString)} is not configured",
        new[] { nameof(ConnectionString) });
    }
  }
}