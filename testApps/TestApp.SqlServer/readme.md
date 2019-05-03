# TestApp.SqlServer

A .NET Core console application to test out how to setup FluentMigrator migrations at a low level.  It shows how the various bits work together and where I might need to add inflection (configuration) points in the flow.

## Running Docker

This program relies on a docker instance for the database.

```
$ cd test/TestApp.SqlServer
$ docker compose up -d
```

