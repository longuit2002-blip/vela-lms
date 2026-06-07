using System.Data.Common;
using Lms.Application.Abstractions;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Lms.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Sets the Postgres session variable <c>app.current_org</c> immediately after a connection opens,
/// so RLS policies (keyed on <c>current_setting('app.current_org', true)</c>) see the current tenant.
/// Re-applied on every physical open; Npgsql's default connection reset (DISCARD ALL) clears it on
/// return to the pool, so no value leaks across tenants. Scoped to the current request's tenant.
/// </summary>
public sealed class TenantConnectionInterceptor(ITenantContext tenant) : DbConnectionInterceptor
{
    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        Apply(connection);
        base.ConnectionOpened(connection, eventData);
    }

    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        await ApplyAsync(connection, cancellationToken);
        await base.ConnectionOpenedAsync(connection, eventData, cancellationToken);
    }

    private void Apply(DbConnection connection)
    {
        if (!tenant.HasTenant)
            return;

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT set_config('app.current_org', @org, false)";
        AddOrgParameter(command);
        command.ExecuteNonQuery();
    }

    private async Task ApplyAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        if (!tenant.HasTenant)
            return;

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT set_config('app.current_org', @org, false)";
        AddOrgParameter(command);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private void AddOrgParameter(DbCommand command)
    {
        var p = command.CreateParameter();
        p.ParameterName = "org";
        p.Value = tenant.OrganizationId.ToString();
        command.Parameters.Add(p);
    }
}
