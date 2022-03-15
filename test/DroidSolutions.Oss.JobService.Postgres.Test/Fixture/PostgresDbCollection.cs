using Xunit;

namespace DroidSolutions.Oss.JobService.Postgres.Test.Fixture;

/// <summary>
/// Empty class that allows to share the same fixture instance among test classes. For more info see
/// <see href="https://xunit.net/docs/shared-context#collection-fixture"></see>.
/// </summary>
[CollectionDefinition("PostgresDb")]
public class PostgresDbCollection : ICollectionFixture<PostgresDbSetup>
{
}
