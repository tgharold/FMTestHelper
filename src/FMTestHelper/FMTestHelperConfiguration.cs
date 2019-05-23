using System;
using System.Data.Common;

namespace FMTestHelper
{
    public class FMTestHelperConfiguration
    {
        public FMTestHelperConfiguration(
            string adminConnectionString,
            DbProviderFactory providerFactory,
            string testDatabaseName,
            string databaseNameKey = "Initial Catalog"
            )
        {
            DbProviderFactory = providerFactory
                ?? throw new ArgumentNullException(nameof(providerFactory));

            AdminConnectionString = adminConnectionString;
            TestDatabaseName = testDatabaseName;
            DatabaseNameKey = databaseNameKey;        
            
            // Use the admin connection string, update it with the test database name
            var testCSB = DbProviderFactory.CreateConnectionStringBuilder();
            testCSB.ConnectionString = AdminConnectionString;
            testCSB[DatabaseNameKey] = TestDatabaseName;
            TestConnectionString = testCSB.ConnectionString;
        }
        
        public DbProviderFactory DbProviderFactory { get; }
        
        /// <summary>Connection string for an account which can connect
        /// and create/destroy databases.</summary>
        public string AdminConnectionString { get; }
        
        public string TestConnectionString { get; }
        
        /// <summary>The key used to find the database name in a
        /// ConnectionStringBuilder collection.  Some database providers
        /// use different keys.</summary>
        public string DatabaseNameKey { get; }
        
        public string TestDatabaseName { get; }
    }
}