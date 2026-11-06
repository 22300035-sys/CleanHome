namespace CleanHome.IntegrationTests;

// These classes migrate the same test database. Serialize their setup while
// preserving the explicit concurrent requests exercised inside each test.
[CollectionDefinition("PostgreSQL")]
public sealed class PostgreSqlCollection;
