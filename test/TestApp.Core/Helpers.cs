using System;
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
                    command.CommandText = $"DROP DATABASE IF EXISTS {configuration.TestDatabaseName}; CREATE DATABASE {configuration.TestDatabaseName}";
                    command.Connection = connection;
                    Console.WriteLine("Opening connection...");
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
                    command.CommandText = $"ALTER DATABASE {configuration.TestDatabaseName} SET OFFLINE WITH ROLLBACK IMMEDIATE;";
                    command.Connection = connection;
                    Console.WriteLine("Opening connection...");
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
                    command.CommandText = $"DROP DATABASE IF EXISTS {configuration.TestDatabaseName};";
                    command.Connection = connection;
                    Console.WriteLine("Opening connection...");
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
                Console.WriteLine($"{nameof(csb)}[{k}]='{csb[k.ToString()]}'");
            Console.WriteLine();
        }
     
    }
}