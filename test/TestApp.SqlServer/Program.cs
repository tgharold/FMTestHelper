using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Threading;
using FluentMigrator.Runner;
using FluentMigrator.Runner.VersionTableInfo;
using Microsoft.EntityFrameworkCore.Storage;
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
            
            //PrintTableColumnsForSysProcesses(dbFactory, adminCSB, databaseName, serverNameKey);
            PrintOpenConnectionList(dbFactory, adminCSB, databaseName, serverNameKey);

            // -------------------- CREATE DATABASE
            // This bit is all done using the dbFactory object, no provider-specific code

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
                    Console.WriteLine($"Execute: ${command.CommandText}");
                    command.ExecuteNonQuery();
                    Console.WriteLine("Done creating test database.");
                }
            }
            
            PrintOpenConnectionList(dbFactory, adminCSB, databaseName, serverNameKey);
            
            // seeing frequent "PAGEIOLATCH_SH" waits after creating the database
            Thread.Sleep(2500);

            PrintOpenConnectionList(dbFactory, adminCSB, databaseName, serverNameKey);

            // -------------------- RUN MIGRATIONS
            
            /* While FluentMigrator can be fed from the dbFactory object,
             * there are bits that are provider-specific like ".AddSqlServer()".
             *
             * There might be a way to figure out the database type from the dbFactory?
             */
            
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

            PrintOpenConnectionList(dbFactory, adminCSB, databaseName, serverNameKey);

            // -------------------- DO TESTS OF MIGRATIONS
            // Tests can probably be written in a provider-agnostic fashion using dbFactory object
            
            Console.WriteLine("Goodbye, World!");
            
            // -------------------- DESTROY DATABASE
            // This bit is all done using the dbFactory object, no provider-specific code

            Console.WriteLine("Destroy test database...");
            using (var connection = dbFactory.CreateConnection())
            {
                Debug.Assert(connection != null, nameof(connection) + " != null");
                connection.ConnectionString = adminCSB.ConnectionString;
                connection.Open();
                
                using (var command = dbFactory.CreateCommand())
                {
                    // SQL Server method of putting the database offline (closing all connections)
                    // It's not possible to parameterize the database names here
                    Debug.Assert(command != null, nameof(command) + " != null");
                    command.CommandText = $"ALTER DATABASE [{databaseName}] SET OFFLINE WITH ROLLBACK IMMEDIATE;";
                    command.Connection = connection;
                    Console.WriteLine($"Opening connection to {adminCSB[serverNameKey]}...");
                    Console.WriteLine($"Execute: ${command.CommandText}");
                    command.ExecuteNonQuery();
                    Console.WriteLine("Done.");
                }

                using (var command = dbFactory.CreateCommand())
                {
                    // It's not possible to parameterize the database names in a DROP
                    Debug.Assert(command != null, nameof(command) + " != null");
                    command.CommandText = $"DROP DATABASE IF EXISTS [{databaseName}];";
                    command.Connection = connection;
                    Console.WriteLine($"Opening connection to {adminCSB[serverNameKey]}...");
                    Console.WriteLine($"Execute: ${command.CommandText}");
                    command.ExecuteNonQuery();
                    Console.WriteLine("Done.");
                }
            }
            
            PrintOpenConnectionList(dbFactory, adminCSB, databaseName, serverNameKey);
        }

        private static void PrintOpenConnectionList(
            DbProviderFactory dbFactory, 
            DbConnectionStringBuilder adminCSB,
            string databaseName, 
            string serverNameKey
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
            
            Console.WriteLine("Look for open connections...");
            using (var connection = dbFactory.CreateConnection())
            {
                Debug.Assert(connection != null, nameof(connection) + " != null");
                connection.ConnectionString = adminCSB.ConnectionString;
                connection.Open();

                using (var command = dbFactory.CreateCommand())
                {
                    Debug.Assert(command != null, nameof(command) + " != null");
                    command.CommandText =
                        $"select DB_NAME(dbid) as DBName, * from sys.sysprocesses where DB_NAME(dbid) = @dbName;";
                    command.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "dbName",
                        Value = databaseName
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

        private static void PrintTableColumnsForSysProcesses(
            DbProviderFactory dbFactory, 
            DbConnectionStringBuilder adminCSB,
            string databaseName, 
            string serverNameKey
            )
        {
            using (var connection = dbFactory.CreateConnection())
            {
                Debug.Assert(connection != null, nameof(connection) + " != null");
                connection.ConnectionString = adminCSB.ConnectionString;
                connection.Open();

                using (var command = dbFactory.CreateCommand())
                {
                    Debug.Assert(command != null, nameof(command) + " != null");
                    command.CommandText =
                        $"select DB_NAME(dbid) as DBName, * from sys.sysprocesses where DB_NAME(dbid) = @dbName;";
                    command.Parameters.Add(new SqlParameter
                    {
                        ParameterName = "dbName",
                        Value = databaseName
                    });
                    command.Connection = connection;

                    using (var reader = command.ExecuteReader())
                    {
                        DataTable schemaTable = reader.GetSchemaTable();

                        foreach (DataRow row in schemaTable.Rows)
                        {
                            foreach (DataColumn column in schemaTable.Columns)
                            {
                                Console.Write(String.Format("[{0}]='{1}' ",
                                    column.ColumnName, row[column]));
                            }

                            Console.WriteLine();
                            Console.WriteLine();
                        }
                    }

                    Console.WriteLine("Done.");
                }
            }
        }
    }
}