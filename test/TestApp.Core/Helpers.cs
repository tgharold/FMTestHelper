using System;
using System.Data;
using System.Data.Common;
using System.Diagnostics;

namespace TestApp.Core
{
    public static class Helpers
    {
        public static void CreateTestDatabase(
            TestDatabaseConfiguration configuration
            )
        {
            Console.WriteLine("Create test database...");
            using (var connection = configuration.DbProviderFactory.CreateConnection())
            {
                Debug.Assert(connection != null, nameof(connection) + " != null");
                connection.ConnectionString = configuration.AdminConnectionString;
                connection.Open();

                using (var command = configuration.DbProviderFactory.CreateCommand())
                {
                    // It's not possible to parameterize the database names in a DROP/CREATE
                    Debug.Assert(command != null, nameof(command) + " != null");
                    command.CommandText = $"DROP DATABASE IF EXISTS [{configuration.TestDatabaseName}]; CREATE DATABASE [{configuration.TestDatabaseName}]";
                    command.Connection = connection;
                    Console.WriteLine($"Opening connection...");
                    Console.WriteLine($"Execute: ${command.CommandText}");
                    command.ExecuteNonQuery();
                    Console.WriteLine("Done creating test database.");
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
                    command.CommandText = $"ALTER DATABASE [{configuration.TestDatabaseName}] SET OFFLINE WITH ROLLBACK IMMEDIATE;";
                    command.Connection = connection;
                    Console.WriteLine($"Opening connection...");
                    Console.WriteLine($"Execute: ${command.CommandText}");
                    command.ExecuteNonQuery();
                    Console.WriteLine("Done.");
                }
            }
        }
        
        public static void DestroyDatabase(
            TestDatabaseConfiguration configuration
            )
        {
            Console.WriteLine("Destroy test database...");
            using (var connection = configuration.DbProviderFactory.CreateConnection())
            {
                Debug.Assert(connection != null, nameof(connection) + " != null");
                connection.ConnectionString = configuration.AdminConnectionString;
                connection.Open();

                using (var command = configuration.DbProviderFactory.CreateCommand())
                {
                    // It's not possible to parameterize the database names in a DROP
                    Debug.Assert(command != null, nameof(command) + " != null");
                    command.CommandText = $"DROP DATABASE IF EXISTS [{configuration.TestDatabaseName}];";
                    command.Connection = connection;
                    Console.WriteLine($"Opening connection...");
                    Console.WriteLine($"Execute: ${command.CommandText}");
                    command.ExecuteNonQuery();
                    Console.WriteLine("Done.");
                }
            }
        }
        
        public static string CreateTestDatabaseConnectionString(
            TestDatabaseConfiguration configuration
            )
        {
            var testCSB = configuration.DbProviderFactory.CreateConnectionStringBuilder();
            Debug.Assert(testCSB != null, nameof(testCSB) + " != null");

            // Use the admin connection string, update it with the test database name
            testCSB.ConnectionString = configuration.AdminConnectionString;
            testCSB[configuration.DatabaseNameKey] = configuration.TestDatabaseName;
            return testCSB.ConnectionString;
        }
        
        public static void PrintOpenConnectionList<TParameter>(
            TestDatabaseConfiguration configuration
            ) where TParameter : DbParameter, new()
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
                        $"select DB_NAME(dbid) as DBName, * from sys.sysprocesses where DB_NAME(dbid) = @dbName;";
                    command.Parameters.Add(new TParameter
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
        
        public static void PrintConnectionStringBuilderKeysAndValues(
            TestDatabaseConfiguration configuration,
            string connectionString
            )
        {
            var csb = configuration.DbProviderFactory.CreateConnectionStringBuilder();
            Debug.Assert(csb != null, nameof(csb) + " != null");

            // Parse the connection string by shoving it into the ConnectionStringBuilder
            csb.ConnectionString = connectionString;
            foreach (var k in csb.Keys)
                Console.WriteLine($"{nameof(csb)}[{k}]='{csb[k.ToString()].ToString()}'");
            Console.WriteLine();
        }
        
        public static void PrintTableColumnsForSysProcesses<TParameter>(
            TestDatabaseConfiguration configuration
            ) where TParameter : DbParameter, new()
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
                        $"select DB_NAME(dbid) as DBName, * from sys.sysprocesses where DB_NAME(dbid) = @dbName;";
                    command.Parameters.Add(new TParameter
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