using System;
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
            TestDatabaseConfiguration configuration
            )
        {
            Helpers.PrintOpenConnectionList<NpgsqlParameter>(configuration);
        }

        private static void PrintTableColumnsForSysProcesses(
            TestDatabaseConfiguration configuration
            )
        {
            Helpers.PrintTableColumnsForSysProcesses<NpgsqlParameter>(configuration);
        }        
    }
}
