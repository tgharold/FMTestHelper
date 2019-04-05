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

            Console.WriteLine("Create database.");

            var adminConnectionStringBuilder = new SqlConnectionStringBuilder
            {
                DataSource = "localhost,11433", 
                UserID = "sa", 
                Password = "Pass123!", 
                InitialCatalog = "master"
            };
            
            var testDatabaseConnectionString = CreateDatabase(adminConnectionStringBuilder, databaseName);
            
            Console.WriteLine("Creating the service provider (DI).");
            var serviceProvider = CreateServices(testDatabaseConnectionString);
            
            Console.WriteLine("Running the migration...");
            // Put the database update into a scope to ensure
            // that all resources will be disposed.
            using (var scope = serviceProvider.CreateScope())
            {
                UpdateDatabase(scope.ServiceProvider);
                //TODO: Need to destroy the MigrationRunner Processor
            }
            
            Console.WriteLine("Goodbye, World!");
            DestroyDatabase(adminConnectionStringBuilder, databaseName);
        }

        private static string CreateDatabase(
            SqlConnectionStringBuilder csb, 
            string databaseName
            )
        {
            using (var connection = new SqlConnection(csb.ConnectionString))
            {
                connection.Open();
                // It's not possible to parameterize the database names
                using (var command = new SqlCommand(
                    $"DROP DATABASE IF EXISTS [{databaseName}]; CREATE DATABASE [{databaseName}]", 
                    connection
                ))
                {
                    Console.WriteLine($"Opening connection to {csb.DataSource}...");
                    command.ExecuteNonQuery();
                    Console.WriteLine("Done.");
                }
            }

            var testBuilder = new SqlConnectionStringBuilder(csb.ConnectionString);
            testBuilder.InitialCatalog = databaseName;
            return testBuilder.ConnectionString;
        }
        
        private static void DestroyDatabase(
            SqlConnectionStringBuilder csb, 
            string databaseName
            )
        {
            using (var connection = new SqlConnection(csb.ConnectionString))
            {
                connection.Open();
                // It's not possible to parameterize the database names
                using (var command = new SqlCommand(
                    $"DROP DATABASE IF EXISTS [{databaseName}];", 
                    connection
                    ))
                {
                    Console.WriteLine($"Opening connection to {csb.DataSource}...");
                    command.ExecuteNonQuery();
                    Console.WriteLine("Done.");
                }
            }
        }        
        
        /// <summary>
        /// Configure the dependency injection services
        /// </summary>
        private static IServiceProvider CreateServices(string connectionString)
        {
            var services = new ServiceCollection();

            services.AddScoped<IVersionTableMetaData, VersionTableMetaData>();

            services
                // Add common FluentMigrator services
                .AddFluentMigratorCore();
                
            services
                .ConfigureRunner(rb => rb
                    .AddSqlServer() // pick which database type to use for the runner
                    .WithGlobalConnectionString(connectionString)
                    .ScanIn(typeof(InitialMigration).Assembly).For.Migrations()
                );
                
            services
                // Enable logging to console in the FluentMigrator way
                .AddLogging(lb => lb.AddFluentMigratorConsole())
                ;
                

            return services.BuildServiceProvider(false);
        }
        
        private static void UpdateDatabase(IServiceProvider serviceProvider)
        {
            var runner = serviceProvider.GetRequiredService<IMigrationRunner>();
            runner.MigrateUp();
        }        
    }
}