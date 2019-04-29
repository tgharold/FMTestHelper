using System;
using System.Data.Common;

namespace TestApp.Core
{
    public class TestDatabaseConfiguration
    {
        public DbProviderFactory DbProviderFactory { get; }

        public TestDatabaseConfiguration(
            DbProviderFactory providerFactory
            )
        {
            DbProviderFactory = providerFactory
                ?? throw new ArgumentNullException(nameof(providerFactory));
        }

        /// <summary>Connection string for an account which can connect
        /// and create/destroy databases.</summary>
        public string AdministratorConnectionString { get; set; }

        public string DatabaseNameKey { get; set; }
        public string TestDatabaseName { get; set; }
    }
}