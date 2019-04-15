using System;
using System.Data.SqlClient;
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

            var databaseName = $"{Guid.NewGuid():N}";
            Console.WriteLine($"databaseName [{databaseName}].");

            // -------------------- CREATE DATABASE

            Console.WriteLine("Create database.");

            var adminConnectionStringBuilder = new SqlConnectionStringBuilder
            {
                DataSource = "localhost,11433", 
                UserID = "sa", 
                Password = "Pass123!", 
                InitialCatalog = "master"
            };

            using (var connection = new SqlConnection(adminConnectionStringBuilder.ConnectionString))
            {
                connection.Open();
                // It's not possible to parameterize the database names
                using (var command = new SqlCommand(
                    $"DROP DATABASE IF EXISTS [{databaseName}]; CREATE DATABASE [{databaseName}]", 
                    connection
                ))
                {
                    Console.WriteLine($"Opening connection to {adminConnectionStringBuilder.DataSource}...");
                    command.ExecuteNonQuery();
                    Console.WriteLine("Done.");
                }
            }

            // -------------------- RUN MIGRATIONS

            var testBuilder = new SqlConnectionStringBuilder(adminConnectionStringBuilder.ConnectionString);
            testBuilder.InitialCatalog = databaseName;
            var testDatabaseConnectionString = testBuilder.ConnectionString;
            
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
                //TODO: Need to destroy the MigrationRunner Processor
            }
            
            // -------------------- DO TESTS OF MIGRATIONS
            
            Console.WriteLine("Goodbye, World!");
            
            // -------------------- DESTROY DATABASE

            using (var connection1 = new SqlConnection(adminConnectionStringBuilder.ConnectionString))
            {
                connection1.Open();
                // It's not possible to parameterize the database names
                using (var command1 = new SqlCommand(
                    $"DROP DATABASE IF EXISTS [{databaseName}];", 
                    connection1
                ))
                {
                    Console.WriteLine($"Opening connection to {adminConnectionStringBuilder.DataSource}...");
                    command1.ExecuteNonQuery();
                    Console.WriteLine("Done.");
                }
            }
        }
    }
}