# TestApp.MariaDB

A .NET Core console application to test out how to setup FluentMigrator migrations at a low level.  It shows how the various bits work together and where I might need to add inflection (configuration) points in the flow.

## Running Docker

This program relies on a docker instance for the database.

```
$ cd test/TestApp.MariaDB
$ docker compose up -d
```

## Notes

- MySQL/MariaDB do not properly support the concept of SQL schemas.  In MySQL 5, "create schema" is just an alias for "create database".



