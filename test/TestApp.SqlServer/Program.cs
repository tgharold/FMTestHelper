using System;
using System.Data.Common;
using System.Data.SqlClient;
using System.Diagnostics;
using FluentMigrator.Runner;
using FluentMigrator.Runner.VersionTableInfo;
using Microsoft.Extensions.DependencyInjection;
using TestApp.SqlServer.Migrations;

namespace TestApp.SqlServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting");
            
            // The assumption is that all we have is a connection string
            // And that connection string must have CREATE DATABASE, etc. permissions
            const string adminConnectionString = "server=localhost,11433;database=master;user=sa;password=Pass123!;";
            
            // Use DbProviderFactory to keep things implementation-agnostic
            // The registration name can be anything!, using nameof() here for convenience
            DbProviderFactories.RegisterFactory(nameof(SqlClientFactory), SqlClientFactory.Instance);
            var dbFactory = DbProviderFactories.GetFactory(nameof(SqlClientFactory));
            
            // Everything after this should depend only on dbFactory (type: DbProviderFactory)

            var adminCSB = dbFactory.CreateConnectionStringBuilder();
            Debug.Assert(adminCSB != null, nameof(adminCSB) + " != null");
            
            // Parse the connection string by shoving it into the ConnectionStringBuilder
            adminCSB.ConnectionString = adminConnectionString;
            foreach(var k in adminCSB.Keys) Console.WriteLine($"{nameof(adminCSB)}[{k}]='{adminCSB[k.ToString()].ToString()}'");
            Console.WriteLine();
            
            // Different databases do connection strings slightly different in terms of what the database/server name is
            const string databaseNameKey = "Initial Catalog";
            const string serverNameKey = "Data Source";

            // Create a random database name, avoid spaces, hyphens, underscores
            var databaseName = $"test{Guid.NewGuid():N}";
            Console.WriteLine($"Test databaseName [{databaseName}].");

            // -------------------- CREATE DATABASE

            Console.WriteLine("Create test database...");
            using (var connection = dbFactory.CreateConnection())
            {
                Debug.Assert(connection != null, nameof(connection) + " != null");
                connection.ConnectionString = adminCSB.ConnectionString;
                connection.Open();
                
                using (var command = dbFactory.CreateCommand())
                {
                    // It's not possible to parameterize the database names in a DROP/CREATE
                    Debug.Assert(command != null, nameof(command) + " != null");
                    command.CommandText = $"DROP DATABASE IF EXISTS [{databaseName}]; CREATE DATABASE [{databaseName}]";
                    command.Connection = connection;
                    Console.WriteLine($"Opening connection to {adminCSB[serverNameKey]}...");
                    command.ExecuteNonQuery();
                    Console.WriteLine("Done creating test database.");
                }
            }

            // -------------------- RUN MIGRATIONS
            
            var testCSB = dbFactory.CreateConnectionStringBuilder();
            Debug.Assert(testCSB != null, nameof(testCSB) + " != null");
            
            // Use the admin connection string, update it with the test database name
            testCSB.ConnectionString = adminConnectionString;
            testCSB[databaseNameKey] = databaseName;
            var testDatabaseConnectionString = testCSB.ConnectionString;
            
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
            var serviceProvider = (IServiceProvider) services.BuildServiceProvider(false);
            
            Console.WriteLine("Running the migration...");
            // Put the database update into a scope to ensure
            // that all resources will be disposed.
            using (var scope = serviceProvider.CreateScope())
            {
                var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
                runner.MigrateUp();
                
                //TODO: Need to destroy the MigrationRunner Processor or whatever is holding connection open
            }
            
            // -------------------- DO TESTS OF MIGRATIONS
            
            Console.WriteLine("Goodbye, World!");
            
            // -------------------- DESTROY DATABASE

            Console.WriteLine("Destroy test database...");
            using (var connection = dbFactory.CreateConnection())
            {
                Debug.Assert(connection != null, nameof(connection) + " != null");
                connection.ConnectionString = adminCSB.ConnectionString;
                connection.Open();
                
                using (var command = dbFactory.CreateCommand())
                {
                    // It's not possible to parameterize the database names in a DROP
                    Debug.Assert(command != null, nameof(command) + " != null");
                    command.CommandText = $"DROP DATABASE IF EXISTS [{databaseName}];";
                    command.Connection = connection;
                    Console.WriteLine($"Opening connection to {adminCSB[serverNameKey]}...");
                    command.ExecuteNonQuery();
                    Console.WriteLine("Done.");
                }
            }
        }
    }
}