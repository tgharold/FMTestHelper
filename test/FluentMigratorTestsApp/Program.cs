using System;
using System.Data.SqlClient;
using FluentMigrator.Runner;
using FluentMigrator.Runner.VersionTableInfo;
using FluentMigratorTestsApp.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace FluentMigratorTestsApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting");

            Console.WriteLine("Create database.");
            var databaseName = CreateDatabase();
            
            
            var migrationBuilder = new SqlConnectionStringBuilder
            {
                DataSource = "localhost,11433",
                UserID = "sa", 
                Password = "Pass123!", 
                InitialCatalog = databaseName
            };
            
            Console.WriteLine("Creating the service provider (DI).");
            var serviceProvider = CreateServices(migrationBuilder.ConnectionString);
            
            Console.WriteLine("Running the migration...");
            // Put the database update into a scope to ensure
            // that all resources will be disposed.
            using (var scope = serviceProvider.CreateScope())
            {
                UpdateDatabase(scope.ServiceProvider);
                //TODO: Need to destroy the MigrationRunner Processor
            }
            
            Console.WriteLine("Goodbye, World!");
            DestroyDatabase(databaseName);
        }

        private static string CreateDatabase()
        {
            var createBuilder = new SqlConnectionStringBuilder
            {
                DataSource = "localhost,11433", 
                UserID = "sa", 
                Password = "Pass123!", 
                InitialCatalog = "master"
            };
            
            var databaseName = $"{Guid.NewGuid():N}";
            Console.WriteLine($"databaseName [{databaseName}].");

            using (var connection = new SqlConnection(createBuilder.ConnectionString))
            {
                connection.Open();
                // It's not possible to parameterize the database names
                using (var command = new SqlCommand(
                    $"DROP DATABASE IF EXISTS [{databaseName}]; CREATE DATABASE [{databaseName}]", 
                    connection
                ))
                {
                    Console.WriteLine($"Opening connection to {createBuilder.DataSource}...");
                    command.ExecuteNonQuery();
                    Console.WriteLine("Done.");
                }
            }

            return databaseName;
        }
        
        private static void DestroyDatabase(string databaseName)
        {
            var destroyBuilder = new SqlConnectionStringBuilder
            {
                DataSource = "localhost,11433", 
                UserID = "sa", 
                Password = "Pass123!", 
                InitialCatalog = "master"
            };
            
            using (var connection = new SqlConnection(destroyBuilder.ConnectionString))
            {
                connection.Open();
                // It's not possible to parameterize the database names
                using (var command = new SqlCommand(
                    $"DROP DATABASE IF EXISTS [{databaseName}];", 
                    connection
                    ))
                {
                    Console.WriteLine($"Opening connection to {destroyBuilder.DataSource}...");
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