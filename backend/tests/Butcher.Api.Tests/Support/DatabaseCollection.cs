namespace Butcher.Api.Tests.Support;

[CollectionDefinition(Name)]
public class DatabaseCollection : ICollectionFixture<PostgresDatabaseFixture>
{
    public const string Name = "Database";
}
