using Microsoft.Extensions.Hosting;

namespace System;

public static class EnvironmentExtensions
{
  public static string GetShortenName(this IHostEnvironment env) => env.EnvironmentName.ToLower() switch
  {
    "production" => "prod",
    "development" => "dev",
    _ => env.EnvironmentName.ToLower(),
  };
}