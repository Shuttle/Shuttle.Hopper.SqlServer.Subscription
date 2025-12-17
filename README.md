# Shuttle.Hopper.SqlServer.Subscription

Sql Server implementation of the `ISubscriptionQuery` interface for use with Shuttle.Hopper.

## Configuration

```c#
services.AddDataAccess(builder =>
{
	builder.AddConnectionString("shuttle", "Microsoft.Data.SqlClient", "server=.;database=shuttle;user id=sa;password=Pass!000");
});

services.AddSqlQueue(builder =>
{
	builder.AddOptions("shuttle", new SqlQueueOptions
	{
		ConnectionStringName = "shuttle"
	});
});
```

The default JSON settings structure is as follows:

```json
{
  "Shuttle": {
    "SqlQueue": {
      "ConnectionStringName": "connection-string-name"
    }
  }
}
``` 

## Options

| Option | Default	| Description |
| --- | --- | --- | 
| `ConnectionStringName` | | The name of the connection string to use.  This package makes use of [Shuttle.Core.Data](https://shuttle.github.io/shuttle-core/data/shuttle-core-data.html) for data access. |
