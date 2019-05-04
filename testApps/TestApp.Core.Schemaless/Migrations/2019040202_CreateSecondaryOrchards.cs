using FluentMigrator;

namespace TestApp.Core.Schemaless.Migrations
{
    [Migration(2019040202)]
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