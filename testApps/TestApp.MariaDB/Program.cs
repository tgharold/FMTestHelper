﻿using System;
using System.Diagnostics;
using System.Threading;
using FluentMigrator.Runner;
using FluentMigrator.Runner.VersionTableInfo;
using FMTestHelper;
using Microsoft.Extensions.DependencyInjection;
using MySql.Data.MySqlClient;
using TestApp.Core;
using TestApp.Core.Schemaless.Migrations;

namespace TestApp.MariaDB
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting");
            Console.WriteLine();

            var configuration = new FMTestHelperConfiguration(
                "server=localhost;port=33060;database=mysql;user=root;password=Woa3abohjoo0doz;",
                MySqlClientFactory.Instance,
                $"test{Guid.NewGuid():N}",
                "Database"
                );

            // print what the Connection String looks like from ConnectionStringBuilder
            Console.WriteLine("Admin connection string:");
            Helpers.PrintConnectionStringBuilderKeysAndValues(configuration, configuration.AdminConnectionString);

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
             * there are bits that are provider-specific like ".AddMySql5()".
             *
             * There might be a way to figure out the database type from the dbFactory?
             */

            var testDatabaseConnectionString = configuration.TestConnectionString;
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
                    .AddMySql5() // pick which database type to use for the runner
                    .WithGlobalConnectionString(testDatabaseConnectionString)
                    .ScanIn(typeof(InitialMigration).Assembly).For.Migrations()
                );

            services
                // Enable logging to console in the FluentMigrator way
                .AddLogging(lb => lb.AddFluentMigratorConsole())
                .Configure<FluentMigratorLoggerOptions>(cfg =>
                {
                    cfg.ShowSql = true;
                    cfg.ShowElapsedTime = true;
                });

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
            CloseAllDatabaseConnections(configuration);
            PrintOpenConnectionList(configuration);

            // Destroy the test database
            Helpers.DestroyDatabase(configuration);
            PrintOpenConnectionList(configuration);
        }

        private static void CloseAllDatabaseConnections(
            FMTestHelperConfiguration configuration
            )
        {
            /* MySQL/MariaDB does not have a safe way to close all
             * connections to a particular database.  Unless you want
             * to create SQL statements on the fly (risky).
             *
             * select concat('CALL mysql.rds_kill( ',id,');')
             * from information_schema.processlist
             * where DB=@dbName;
             *
             * Then process the results, feeding those into execution
             */
        }

        private static void PrintOpenConnectionList(
            FMTestHelperConfiguration configuration
            )
        {
            var sql = $@"
SELECT *
FROM INFORMATION_SCHEMA.PROCESSLIST
WHERE DB = @dbName;
;";

            Console.WriteLine();
            Console.WriteLine($"Look for open connections to [{configuration.TestDatabaseName}]...");
            using (var connection = configuration.DbProviderFactory.CreateConnection())
            {
                Debug.Assert(connection != null, nameof(connection) + " != null");
                connection.ConnectionString = configuration.AdminConnectionString;
                connection.Open();

                using (var command = configuration.DbProviderFactory.CreateCommand())
                {
                    Debug.Assert(command != null, nameof(command) + " != null");
                    command.CommandText = sql;
                    command.Parameters.Add(new MySqlParameter
                    {
                        ParameterName = "dbName",
                        Value = configuration.TestDatabaseName
                    });
                    command.Connection = connection;

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                Console.Write($"{reader["db"]}\t");
                                Console.Write($"{reader["user"]}\t");
                                Console.Write($"{reader["id"]}\t");
                                Console.Write($"{reader["command"]}\t");
                                Console.Write($"{reader["state"]}\t");
                                Console.Write($"{reader["info"]}\t");
                                Console.Write($"{reader["time"]}");
                                Console.WriteLine();
                            }
                        }
                    }
                    Console.WriteLine();
                }
            }
        }
    }
}
