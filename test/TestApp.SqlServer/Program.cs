using System;
using System.Data.Common;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Threading;
using FluentMigrator.Runner;
using FluentMigrator.Runner.VersionTableInfo;
using Microsoft.Extensions.DependencyInjection;
using TestApp.Core;
using TestApp.Core.Migrations;

namespace TestApp.SqlServer
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Starting");
            
            // The assumption is that all we have is a connection string
            // And that connection string must have CREATE DATABASE, etc. permissions
            const string adminConnectionString = "server=localhost,14330;database=master;user=sa;password=paNg2aeshohl;";
            
            // Use DbProviderFactory to keep things implementation-agnostic
            // The registration name can be anything!, using nameof() here for convenience
            DbProviderFactories.RegisterFactory(nameof(SqlClientFactory), SqlClientFactory.Instance);
            var dbFactory = DbProviderFactories.GetFactory(nameof(SqlClientFactory));
            
            // Everything after this should depend only on dbFactory (type: DbProviderFactory)

            // print what the Connection String looks like from ConnectionStringBuilder
            Helpers.PrintConnectionStringBuilderKeysAndValues(dbFactory, adminConnectionString);

            // Different databases do connection strings slightly different in terms of what the database/server name is
            const string databaseNameKey = "Initial Catalog";
            const string serverNameKey = "Data Source";

            // Create a random database name, avoid spaces, hyphens, underscores
            var databaseName = $"test{Guid.NewGuid():N}";
            Console.WriteLine($"Test databaseName [{databaseName}].");
            
            //PrintTableColumnsForSysProcesses(dbFactory, adminCSB, databaseName, serverNameKey);
            PrintOpenConnectionList(dbFactory, adminConnectionString, databaseName, serverNameKey);

            // -------------------- CREATE DATABASE
            // This bit is all done using the dbFactory object, no provider-specific code

            Helpers.CreateTestDatabase(dbFactory, adminConnectionString, databaseName);
            PrintOpenConnectionList(dbFactory, adminConnectionString, databaseName, serverNameKey);

            // seeing frequent "PAGEIOLATCH_SH" waits after creating the database, try waiting
            // this could be just SQL/Docker performance that needs a bit to settle down
            Thread.Sleep(2500);
            PrintOpenConnectionList(dbFactory, adminConnectionString, databaseName, serverNameKey);

            // -------------------- RUN MIGRATIONS
            
            /* While FluentMigrator can be fed from the dbFactory object,
             * there are bits that are provider-specific like ".AddSqlServer()".
             *
             * There might be a way to figure out the database type from the dbFactory?
             */
            
            var testDatabaseConnectionString = Helpers.CreateTestDatabaseConnectionString(
                dbFactory, 
                adminConnectionString, 
                databaseNameKey, 
                databaseName
                );

            Console.WriteLine("Creating the service provider (DI).");
            var services = new ServiceCollection();

            services.AddScoped<IVersionTableMetaData, VersionTableMetaData>();

            services
                // Add common FluentMigrator services
                .AddFluentMigratorCore();
                
            services
                .ConfigureRunner(rb => rb
                    .AddSqlServer() // pick which database type to use for the runner
                    .WithGlobalConnectionString(testDatabaseConnectionString)
                    .ScanIn(typeof(InitialMigration).Assembly).For.Migrations()
                );
            
            services
                // Enable logging to console in the FluentMigrator way
                .AddLogging(lb => lb.AddFluentMigratorConsole())
                ;

            using (var serviceProvider = services.BuildServiceProvider(false))
            {

                Console.WriteLine("Running the migration...");
                // Put the database update into a scope to ensure
                // that all resources will be disposed.
                using (var scope = serviceProvider.CreateScope())
                {
                    var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
                    runner.MigrateUp();

                    //TODO: Need to destroy the MigrationRunner Processor or whatever is holding connection open
                }
            }

            PrintOpenConnectionList(dbFactory, adminConnectionString, databaseName, serverNameKey);

            // Sleep for a bit to see if the connection closes on its own
            Thread.Sleep(2500);
            PrintOpenConnectionList(dbFactory, adminConnectionString, databaseName, serverNameKey);

            // -------------------- DO TESTS OF MIGRATIONS
            // Tests can probably be written in a provider-agnostic fashion using dbFactory object
            
            Console.WriteLine("Goodbye, World!");
            
            // -------------------- DESTROY DATABASE
            // This bit is all done using the dbFactory object, no provider-specific code

            // Force closing database connections is a workaround for a bug in FM 3.2.1, but may be needed for broken tests
            Helpers.CloseAllDatabaseConnections(dbFactory, adminConnectionString, databaseName);
            PrintOpenConnectionList(dbFactory, adminConnectionString, databaseName, serverNameKey);

            // Destroy the test database
            Helpers.DestroyDatabase(dbFactory, adminConnectionString, databaseName);
            PrintOpenConnectionList(dbFactory, adminConnectionString, databaseName, serverNameKey);
        }

        private static void PrintOpenConnectionList(
            DbProviderFactory dbFactory, 
            string adminConnectionString,
            string databaseName, 
            string serverNameKey
            )
        {
            Helpers.PrintOpenConnectionList<SqlParameter>(
                dbFactory,
                adminConnectionString,
                databaseName,
                serverNameKey
                );
        }

        private static void PrintTableColumnsForSysProcesses(
            DbProviderFactory dbFactory, 
            string adminConnectionString,
            string databaseName, 
            string serverNameKey
            )
        {
            Helpers.PrintTableColumnsForSysProcesses<SqlParameter>(
                dbFactory,
                adminConnectionString,
                databaseName,
                serverNameKey
            );
        }
    }
}