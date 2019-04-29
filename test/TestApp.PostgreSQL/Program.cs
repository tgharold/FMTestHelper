using System;
using System.Data.Common;
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
            string adminConnectionString,
            string databaseName, 
            string serverNameKey
            )
        {
            Helpers.PrintOpenConnectionList<NpgsqlParameter>(
                dbFactory,
                adminConnectionString,
                databaseName,
                serverNameKey
            );
        }

        private static void PrintTableColumnsForSysProcesses(
            DbProviderFactory dbFactory, 
            string adminConnectionString,
            string databaseName, 
            string serverNameKey
            )
        {
            Helpers.PrintTableColumnsForSysProcesses<NpgsqlParameter>(
                dbFactory,
                adminConnectionString,
                databaseName,
                serverNameKey
            );
        }        
    }
}
