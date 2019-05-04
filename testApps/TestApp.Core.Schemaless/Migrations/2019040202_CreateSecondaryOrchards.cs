using FluentMigrator;
using TestApp.Core.Migrations;

namespace TestApp.Core.Schemaless.Migrations
{
    [Migration(MigrationNumbers.CreateSecondaryOrchards)]
    public class CreateSecondaryOrchards : ForwardOnlyMigration
    {
        public override void Up()
        {
            var schema = Constants.SecondarySchema;

            Create.Table($"{schema}-Orchards")
                .WithColumn("OrchardId").AsInt32().NotNullable().Identity().PrimaryKey()
                .WithColumn("Name").AsString().NotNullable()
                .WithColumn("Description").AsString().NotNullable()
                ;
        }
    }
}