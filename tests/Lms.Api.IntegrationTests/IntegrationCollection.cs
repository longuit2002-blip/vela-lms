namespace Lms.Api.IntegrationTests;

/// <summary>Shares one <see cref="WebAppFactory"/> (and its container) across the endpoint tests.</summary>
[CollectionDefinition(nameof(IntegrationCollection))]
public sealed class IntegrationCollection : ICollectionFixture<WebAppFactory>;
