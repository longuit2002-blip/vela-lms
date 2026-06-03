using System.Reflection;
using Lms.Domain.SeedWork;
using NetArchTest.Rules;

namespace Lms.Architecture.Tests;

/// <summary>
/// Enforces the Clean Architecture dependency rule (ADR-001/002). These guard the layering
/// from commit one — a forbidden reference turns the relevant test red (and fails CI, AE4).
/// </summary>
public class DependencyRuleTests
{
    private static readonly Assembly DomainAssembly = typeof(Entity).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(Lms.Application.DependencyInjection).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(Lms.Infrastructure.DependencyInjection).Assembly;

    private const string Application = "Lms.Application";
    private const string Infrastructure = "Lms.Infrastructure";
    private const string Api = "Lms.Api";

    [Fact]
    public void Domain_should_not_depend_on_other_layers()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(Application, Infrastructure, Api)
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    [Fact]
    public void Application_should_not_depend_on_infrastructure_or_api()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(Infrastructure, Api)
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    [Fact]
    public void Application_should_not_depend_on_efcore_aspnetcore_or_npgsql()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOnAny("Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore", "Npgsql")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    [Fact]
    public void Infrastructure_should_not_depend_on_api()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(Api)
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    private static string Describe(NetArchTest.Rules.TestResult result) =>
        result.IsSuccessful
            ? "OK"
            : "Dependency-rule violation in types: " +
              string.Join(", ", result.FailingTypes.Select(t => t.FullName));
}
