using Xunit;

namespace FluentMigratorTests.Tests
{

    public class SqlClientDatabaseCollection : ICollectionFixture<SqlClientDatabaseFixture>
    {
        public const string Name = nameof(SqlClientDatabaseCollection);
    }
}