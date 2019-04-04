using System;
using System.Data.SqlClient;

namespace FluentMigratorTests.Tests
{
    public class SqlClientDatabaseFixture : IDisposable
    {
        private readonly string _databaseName;
        private readonly SqlConnectionStringBuilder _adminBuilder;

        public SqlClientDatabaseFixture(
            string adminConnectionString,
            string databaseName
            )
        {
            _adminBuilder = new SqlConnectionStringBuilder(adminConnectionString);
            _databaseName = databaseName;
            CreateDatabase();
        }
        
        public SqlClientDatabaseFixture(
            SqlConnectionStringBuilder adminBuilder,
            string databaseName
            )
        {
            _adminBuilder = adminBuilder;
            _databaseName = databaseName;
            CreateDatabase();
        }

        private void CreateDatabase()
        {
            using (var connection = new SqlConnection(_adminBuilder.ConnectionString))
            {
                // It's not possible to parameterize the database names for DROP/CREATE
                using (var command = new SqlCommand(
                    $"DROP DATABASE IF EXISTS [{_databaseName}]; CREATE DATABASE [{_databaseName}]",
                    connection
                ))
                {
                    command.Connection.Open();
                    command.ExecuteNonQuery();
                    command.Connection.Close();
                }
            }
        }

        public void Dispose()
        {
            using (var connection = new SqlConnection(_adminBuilder.ConnectionString))
            {
                // It's not possible to parameterize the database names for DROP/CREATE
                using (var command = new SqlCommand(
                    $"DROP DATABASE IF EXISTS [{_databaseName}];", 
                    connection
                    ))
                {
                    command.Connection.Open();
                    command.ExecuteNonQuery();
                    command.Connection.Close();
                }
            }
        }
    }
}