using FluentMigrator;

namespace TestApp.Core.Migrations
{
    [Migration(2019040202)]
    public class CreateSecondaryOrchards : ForwardOnlyMigration
    {
        public override void Up()
        {
            Create.Schema(Constants.SecondarySchema);
            
            Create.Table("Orchards")
                .InSchema(Constants.SecondarySchema)
                .WithColumn("OrchardId").AsInt32().NotNullable().Identity().PrimaryKey()
                .WithColumn("Name").AsString().NotNullable()
                .WithColumn("Description").AsString().NotNullable()
                ;
        }
    }
}