namespace FSH.WebApi.Application.Multitenancy;

public class CreateTenantRequestValidator : CustomValidator<CreateTenantRequest>
{
    public CreateTenantRequestValidator(
        ITenantService tenantService,
        IStringLocalizer<CreateTenantRequestValidator> T,
        IConnectionStringValidator connectionStringValidator)
    {
        RuleFor(t => t.Id).Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (id, _) => !await tenantService.ExistsWithIdAsync(id))
                .WithMessage((_, id) => T["Tenant {0} already exists.", id]);

        RuleFor(t => t.Name).Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (name, _) => !await tenantService.ExistsWithNameAsync(name!))
                .WithMessage((_, name) => T["Tenant {0} already exists.", name]);

        RuleFor(t => t.DatabaseName).Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (cs, _) => !await tenantService.DatabaseExistAsync(cs!))
                .WithMessage((_, cs) => T["Database {0} already exists.", cs]);

        RuleFor(t => t.AdminEmail).Cascade(CascadeMode.Stop)
            .NotEmpty()
            .EmailAddress();
    }
}