using FluentMigrator;
using TestApp.Core.Migrations;

namespace TestApp.Core.Schemaless.Migrations
{
    [Migration(MigrationNumbers.InitialMigration)]
    public class InitialMigration : ForwardOnlyMigration
    {
        public override void Up()
        {
            var schema = Constants.Schema;

            Create.Table($"{schema}-Orchards")
                .WithColumn("OrchardId").AsInt32().NotNullable().Identity().PrimaryKey()
                .WithColumn("Name").AsString().NotNullable()
                .WithColumn("Description").AsString().NotNullable()
                ;
        }
    }
}