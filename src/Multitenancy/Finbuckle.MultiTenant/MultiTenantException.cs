// Copyright Finbuckle LLC, Andrew White, and Contributors.
// Refer to the solution LICENSE file for more inforation.

using System;

namespace Finbuckle.MultiTenant
{
  public class MultiTenantException : Exception
  {
    public MultiTenantException(string? message) : base(message)
    {
    }

    public MultiTenantException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
  }
}