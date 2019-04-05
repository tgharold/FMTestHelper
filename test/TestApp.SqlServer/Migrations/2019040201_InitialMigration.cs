using FluentMigrator;

namespace TestApp.SqlServer.Migrations
{
    [Migration(2019040201)]
    public class InitialMigration : ForwardOnlyMigration
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
            
            Create.Schema(Constants.SecondarySchema);
            
            Create.Table("Orchards")
                .InSchema(Constants.SecondarySchema)
                .WithColumn("OrchardId").AsInt32().NotNullable().Identity()
                .WithColumn("Name").AsString().NotNullable()
                .WithColumn("Description").AsString().NotNullable()
                ;
        }
    }
}