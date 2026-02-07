# Shuttle.Hopper.SqlServer.Subscription

Sql Server storage of subscriptions for use with Shuttle.Hopper.

> [!NOTE]
> The implementation will automatically create the required database tables if they are missing. The identity used to connect to the database must have sufficient permissions to create schemas and tables.


## Installation

```bash
dotnet add package Shuttle.Hopper.SqlServer.Subscription
```

## Configuration

In order to use Sql Server for subscriptions, you can use the `UseSqlServerSubscription` extension method:

```c#
services.AddHopper(builder =>
{
    builder.UseSqlServerSubscription(options =>
    {
        options.ConnectionString = "server=.;database=shuttle;user id=sa;password=Pass!000";
    });
});
```

The default JSON settings structure is as follows:

```json
{
  "Shuttle": {
    "SqlServer": {
      "Subscription": {
        "ConnectionString": "connection-string",
        "Schema": "dbo",
        "ConfigureDatabase": true
      }
    }
  }
}
``` 

## Options

| Option | Default | Description |
| --- | --- | --- | 
| `ConnectionString` | | The connection string to use for the Sql Server database. |
| `Schema` | `dbo` | The schema to use for the subscription tables. |
| `ConfigureDatabase` | `true` | A boolean value indicating whether the database should be configured (e.g., creating tables) if it doesn't exist. |

