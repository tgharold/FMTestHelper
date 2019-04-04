using FluentMigrator.Runner.Conventions;
using FluentMigrator.Runner.Initialization;
using FluentMigrator.Runner.VersionTableInfo;
using Microsoft.Extensions.Options;

namespace FluentMigratorTestsApp
{
    [VersionTableMetaData]
    public class VersionTableMetaData : DefaultVersionTableMetaData
    {
        // https://github.com/fluentmigrator/fluentmigrator/issues/933
        
        public VersionTableMetaData(
            IConventionSet conventionSet, 
            IOptions<RunnerOptions> runnerOptions
            )
            : base(conventionSet, runnerOptions)
        {
        }

        public override string SchemaName => Constants.Schema;
    }
}