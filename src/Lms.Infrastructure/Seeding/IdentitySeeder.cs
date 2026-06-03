using Lms.Application.Abstractions;
using Lms.Application.Auth;
using Lms.Domain.Organizations;
using Lms.Domain.Users;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lms.Infrastructure.Seeding;

/// <summary>
/// Provisions the first Organization and its OrgOwner from configuration (dev convenience — replaces
/// the retired open org-create). Idempotent: re-running with an existing owner is a no-op. The owner
/// is created with <c>mustChangePassword = true</c> so the seeded default password can't be used past
/// first login. Runs as the app's DB connection (superuser, RLS-bypassing) so the insert is unblocked.
/// </summary>
public sealed class IdentitySeeder(
    IOrganizationRepository organizations,
    IUserRepository users,
    IPasswordHasher passwordHasher,
    IIdGenerator idGenerator,
    IOptions<SeedOptions> options,
    ILogger<IdentitySeeder> logger)
{
    public const string OrgOwnerRole = "OrgOwner";

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var seed = options.Value;
        if (!seed.Enabled)
            return;

        // Idempotent on the owner: if it already exists, there is nothing to do.
        if (await users.FindByEmailForLoginAsync(seed.OwnerEmail, cancellationToken) is not null)
        {
            logger.LogInformation("Identity seed skipped — owner {Email} already exists.", seed.OwnerEmail);
            return;
        }

        var organization = await organizations.FindBySlugAsync(seed.OrganizationSlug, cancellationToken);
        if (organization is null)
        {
            organization = Organization.Create(idGenerator.NewId(), seed.OrganizationName, seed.OrganizationSlug);
            await organizations.AddAsync(organization, cancellationToken);
            await organizations.SaveChangesAsync(cancellationToken);
        }

        var owner = User.Create(
            idGenerator.NewId(),
            organization.Id,
            seed.OwnerEmail,
            passwordHasher.Hash(seed.OwnerPassword),
            [OrgOwnerRole],
            mustChangePassword: true);

        await users.AddAsync(owner, cancellationToken);
        await users.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Identity seed created organization {Slug} and OrgOwner {Email}.", organization.Slug, owner.Email);
    }
}
