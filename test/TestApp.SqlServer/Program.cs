using System;
using System.Data;
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
            Console.WriteLine();

            var configuration = new TestDatabaseConfiguration(
                "server=localhost,14330;database=master;user=sa;password=paNg2aeshohl;",
                SqlClientFactory.Instance,
                $"test{Guid.NewGuid():N}",
                "Initial Catalog"
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

        /// <summary>This is SQLServer specific.</summary>
        public static void PrintOpenConnectionList(
            TestDatabaseConfiguration configuration
            )
        {
            /* Useful column names in sys.sysprocesses:
             * - DB_NAME(dbid) - database name
             * - kpid
             * - lastwaittype
             * - cmd
             * - status
             * - last_batch
             */

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
                    command.CommandText =
                        "select DB_NAME(dbid) as DBName, * from sys.sysprocesses where DB_NAME(dbid) = @dbName;";
                    command.Parameters.Add(new SqlParameter
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
                                Console.Write($"{reader["DBName"]}\t");
                                Console.Write($"{reader["last_batch"]}\t");
                                Console.Write($"{reader["kpid"]}\t");
                                Console.Write($"{reader["lastwaittype"]}\t");
                                Console.Write($"{reader["cmd"]}\t");
                                Console.Write($"{reader["status"]}");
                                Console.WriteLine();
                            }
                        }
                    }
                    Console.WriteLine();
                }
            }
        }        

        
        /// <summary>Displays the list of column names for sys.processes in SQL Server</summary>
        public static void PrintTableColumnsForSysProcesses(
            TestDatabaseConfiguration configuration
            )
        {
            using (var connection = configuration.DbProviderFactory.CreateConnection())
            {
                Debug.Assert(connection != null, nameof(connection) + " != null");
                connection.ConnectionString = configuration.AdminConnectionString;
                connection.Open();

                using (var command = configuration.DbProviderFactory.CreateCommand())
                {
                    Debug.Assert(command != null, nameof(command) + " != null");
                    command.CommandText =
                        "select DB_NAME(dbid) as DBName, * from sys.sysprocesses where DB_NAME(dbid) = @dbName;";
                    command.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "dbName",
                        Value = configuration.TestDatabaseName
                    });
                    command.Connection = connection;

                    using (var reader = command.ExecuteReader())
                    {
                        DataTable schemaTable = reader.GetSchemaTable();

                        foreach (DataRow row in schemaTable.Rows)
                        {
                            foreach (DataColumn column in schemaTable.Columns)
                            {
                                Console.Write("[{0}]='{1}' ", column.ColumnName, row[column]);
                            }

                            Console.WriteLine();
                            Console.WriteLine();
                        }
                    }

                    Console.WriteLine("Done.");
                }
            }
        }  
        
        public static void CloseAllDatabaseConnections(
            TestDatabaseConfiguration configuration
        )
        {
            Console.WriteLine("Close connections to test database...");
            using (var connection = configuration.DbProviderFactory.CreateConnection())
            {
                Debug.Assert(connection != null, nameof(connection) + " != null");
                connection.ConnectionString = configuration.AdminConnectionString;
                connection.Open();

                using (var command = configuration.DbProviderFactory.CreateCommand())
                {
                    // SQL Server method of putting the database offline (closing all connections)
                    // It's not possible to parameterize the database names here
                    Debug.Assert(command != null, nameof(command) + " != null");
                    command.CommandText = $"ALTER DATABASE {configuration.TestDatabaseName} SET OFFLINE WITH ROLLBACK IMMEDIATE;";
                    command.Connection = connection;
                    Console.WriteLine("Opening connection...");
                    Console.WriteLine($"Execute: ${command.CommandText}");
                    command.ExecuteNonQuery();
                    Console.WriteLine("Done.");
                }
            }
        }
    }
}