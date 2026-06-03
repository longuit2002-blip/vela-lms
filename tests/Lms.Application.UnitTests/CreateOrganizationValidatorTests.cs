using Lms.Application.Organizations.Commands.CreateOrganization;

namespace Lms.Application.UnitTests;

public class CreateOrganizationValidatorTests
{
    private readonly CreateOrganizationValidator _validator = new();

    [Theory]
    [InlineData("", "valid-slug")]      // blank name
    [InlineData("   ", "valid-slug")]   // whitespace name
    [InlineData("Acme", "")]            // blank slug
    [InlineData("Acme", "has space")]   // slug with space
    [InlineData("Acme", "under_score")] // slug with underscore
    public void Invalid_inputs_fail_validation(string name, string slug)
    {
        var result = _validator.Validate(new CreateOrganizationCommand(name, slug));
        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData("Acme", "acme")]
    [InlineData("Acme Corp", "Acme-Corp")]   // mixed case allowed (domain normalizes)
    public void Valid_inputs_pass_validation(string name, string slug)
    {
        var result = _validator.Validate(new CreateOrganizationCommand(name, slug));
        Assert.True(result.IsValid);
    }
}
