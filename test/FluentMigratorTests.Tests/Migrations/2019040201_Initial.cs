using FluentMigrator;

namespace FluentMigratorTests.Tests.Migrations
{
    [Migration(2019040201)]
    public class Initial : ForwardOnlyMigration
    {
        public override void Up()
        {
            Create.Schema(Constants.Schema);

            Create.Table("Orchards")
                .InSchema(Constants.Schema)
                .WithColumn("OrchardId").AsInt32().NotNullable().Identity()
                .WithColumn("Name").AsString().NotNullable()
                .WithColumn("Description").AsString().NotNullable()
                ;
        }
    }
}