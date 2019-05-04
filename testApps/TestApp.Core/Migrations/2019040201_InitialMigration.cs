using FluentMigrator;

namespace TestApp.Core.Migrations
{
    [Migration(MigrationNumbers.InitialMigration)]
    public class InitialMigration : ForwardOnlyMigration
    {
        public override void Up()
        {
            Create.Schema(Constants.Schema);

            Create.Table("Orchards")
                .InSchema(Constants.Schema)
                .WithColumn("OrchardId").AsInt32().NotNullable().Identity().PrimaryKey()
                .WithColumn("Name").AsString().NotNullable()
                .WithColumn("Description").AsString().NotNullable()
                ;
        }
    }
}