using System;
using System.Data;
using System.Data.Common;
using System.Diagnostics;

namespace TestApp.Core
{
    public static class Helpers
    {
        public static void PrintOpenConnectionList<TParameter>(
            DbProviderFactory dbFactory, 
            DbConnectionStringBuilder adminCSB,
            string databaseName, 
            string serverNameKey
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
            Console.WriteLine($"Look for open connections to [{databaseName}]...");
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
                    command.Parameters.Add(new TParameter
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
        
        public static void PrintTableColumnsForSysProcesses<TParameter>(
            DbProviderFactory dbFactory, 
            DbConnectionStringBuilder adminCSB,
            string databaseName, 
            string serverNameKey
            ) where TParameter : DbParameter, new()
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
                    command.Parameters.Add(new TParameter
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