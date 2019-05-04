using FluentMigrator;

namespace TestApp.Core.Schemaless.Migrations
{
    [Migration(2019040201)]
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