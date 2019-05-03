# testApps

This is a collection of console applications that demonstrate how to:

- Create a test database
- Wire up FluentMigrator 3.x in .NET Core
- Execute the migrations
- Tear down the test database

There are few external dependencies and should serve as an alternative example for how to use FluentMigration in .NET Core.

## Projects

- `TestApp.Core`: Everything that is database agnostic.
- `TestApp.*`: Database-specific code and the console programs.

