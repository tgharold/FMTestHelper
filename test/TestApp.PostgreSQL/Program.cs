using System;
using System.Threading;
using FluentMigrator.Runner;
using FluentMigrator.Runner.VersionTableInfo;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using TestApp.Core;
using TestApp.Core.Migrations;

namespace TestApp.PostgreSQL
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting");
            Console.WriteLine();

            var configuration = new TestDatabaseConfiguration(
                "host=localhost;port=54320;database=postgres;username=postgres;password=osukos7EnohK;",
                NpgsqlFactory.Instance,
                $"test{Guid.NewGuid():N}",
                "Database"
                );

            // print what the Connection String looks like from ConnectionStringBuilder
            Console.WriteLine("Admin connection string:");
            Helpers.PrintConnectionStringBuilderKeysAndValues(configuration, configuration.AdminConnectionString);

            PrintTableColumnsForSysProcesses(configuration);
            PrintOpenConnectionList(configuration);

            // -------------------- CREATE DATABASE
            // This bit is all done using the dbFactory object, no provider-specific code

            Helpers.CreateTestDatabase(configuration);
            PrintOpenConnectionList(configuration);

            // seeing frequent "PAGEIOLATCH_SH" waits after creating the database, try waiting
            // this could be just SQL/Docker performance that needs a bit to settle down
            Thread.Sleep(2500);
            PrintOpenConnectionList(configuration);

            // -------------------- RUN MIGRATIONS
            
            /* While FluentMigrator can be fed from the dbFactory object,
             * there are bits that are provider-specific like ".AddSqlServer()".
             *
             * There might be a way to figure out the database type from the dbFactory?
             */
            
            var testDatabaseConnectionString = Helpers.CreateTestDatabaseConnectionString(configuration);
            Console.WriteLine("Test database connection string:");
            Helpers.PrintConnectionStringBuilderKeysAndValues(configuration, testDatabaseConnectionString);

            Console.WriteLine("Creating the service provider (DI).");
            var services = new ServiceCollection();

            services.AddScoped<IVersionTableMetaData, VersionTableMetaData>();

            services
                // Add common FluentMigrator services
                .AddFluentMigratorCore();
                
            services
                .ConfigureRunner(rb => rb
                    .AddPostgres() // pick which database type to use for the runner
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

            PrintOpenConnectionList(configuration);

            // Sleep for a bit to see if the connection closes on its own
            Thread.Sleep(2500);
            PrintOpenConnectionList(configuration);

            // -------------------- DO TESTS OF MIGRATIONS
            // Tests can probably be written in a provider-agnostic fashion using dbFactory object
            
            Console.WriteLine("Goodbye, World!");
            Console.WriteLine();
            
            // -------------------- DESTROY DATABASE
            // This bit is all done using the dbFactory object, no provider-specific code

            // Force closing database connections is a workaround for a bug in FM 3.2.1, but may be needed for broken tests
            Helpers.CloseAllDatabaseConnections(configuration);
            PrintOpenConnectionList(configuration);

            // Destroy the test database
            Helpers.DestroyDatabase(configuration);
            PrintOpenConnectionList(configuration);
        }

        private static void PrintOpenConnectionList(
            TestDatabaseConfiguration configuration
            )
        {
            //TODO:
        }

        private static void PrintTableColumnsForSysProcesses(
            TestDatabaseConfiguration configuration
            )
        {
            //TODO:
        }        
    }
}
