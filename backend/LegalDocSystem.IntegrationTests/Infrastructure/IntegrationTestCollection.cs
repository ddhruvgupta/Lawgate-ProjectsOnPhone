using Xunit;

namespace LegalDocSystem.IntegrationTests.Infrastructure;

[CollectionDefinition("Integration")]
public class IntegrationTestCollection : ICollectionFixture<TestWebAppFactory>
{
    // This class has no code — it's just a placeholder for xunit's collection fixture mechanism.
}
