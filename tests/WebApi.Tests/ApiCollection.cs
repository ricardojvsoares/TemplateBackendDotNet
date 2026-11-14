using Xunit;

namespace WebApi.Tests;

[CollectionDefinition(Name)]
public sealed class ApiSuite : ICollectionFixture<ApiFixture>
{
    public const string Name = "api";
}
