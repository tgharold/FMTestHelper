using System;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using Npgsql;
using TestApp.Core;

namespace TestApp.PostgreSQL
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
        }

        private static void PrintOpenConnectionList(
            DbProviderFactory dbFactory, 
            DbConnectionStringBuilder adminCSB,
            string databaseName, 
            string serverNameKey
            )
        {
            Helpers.PrintOpenConnectionList<NpgsqlParameter>(
                dbFactory,
                adminCSB,
                databaseName,
                serverNameKey
            );
        }

        private static void PrintTableColumnsForSysProcesses(
            DbProviderFactory dbFactory, 
            DbConnectionStringBuilder adminCSB,
            string databaseName, 
            string serverNameKey
            )
        {
            Helpers.PrintTableColumnsForSysProcesses<NpgsqlParameter>(
                dbFactory,
                adminCSB,
                databaseName,
                serverNameKey
            );
        }        
    }
}
