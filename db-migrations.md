dotnet ef migrations add "AddTenantSubscriptions" --project .././Migrators/Migrators.MySQL/ --context TenantDbContext -o Migrations/Tenant


dotnet ef migrations remove --project .././Migrators/Migrators.MySQL/ --context TenantDbContext