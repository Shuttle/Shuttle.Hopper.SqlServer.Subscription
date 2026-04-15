# Shuttle.Hopper.SqlServer.Subscription

Sql Server storage of subscriptions for use with Shuttle.Hopper.

> [!NOTE]
> The implementation will automatically create the required database schema and tables if they are missing. The identity used to connect to the database must have sufficient permissions to create schemas and tables.

## Installation

```bash
dotnet add package Shuttle.Hopper.SqlServer.Subscription
```

## Configuration

Configure both the Hopper options (inbox URI and subscription message types) and the Sql Server subscription options:

```c#
services
    .AddHopper(options =>
    {
        options.Inbox.WorkTransportUri = new Uri("queue://./inbox");
        options.Subscription.MessageTypes = new[] { typeof(MyMessage).FullName! };
    })
    .UseSqlServerSubscription(options =>
    {
        options.ConnectionString = "server=.;database=shuttle;user id=sa;password=Pass!000";
    });
```

### Subscription Modes

The `SubscriptionMode` option controls how missing subscriptions are handled:

| Mode | Behavior |
|------|----------|
| `Standard` | Registers the subscriber for all configured message types (default) |
| `Disabled` | Skips subscription registration entirely |
| `FailWhenMissing` | Throws `ApplicationException` if any configured message type is not already subscribed |

```c#
options.Subscription.Mode = SubscriptionMode.FailWhenMissing;
```

### JSON Configuration

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
| `ConnectionString` | *(required)* | The connection string to use for the Sql Server database. Validated at startup. |
| `Schema` | `dbo` | The schema to use for the subscription tables. Must be a valid SQL identifier matching pattern `^[a-zA-Z_][a-zA-Z0-9_]*$`. |
| `ConfigureDatabase` | `true` | A boolean value indicating whether the database schema and tables should be created if they don't exist. |

## Database Schema

When `ConfigureDatabase` is `true`, the following objects are created automatically using a distributed lock to ensure thread-safe creation:

- **Schema**: As specified by the `Schema` option (default: `dbo`)
- **Table**: `SubscriberMessageType` with the following structure:

| Column | Type | Constraints |
|--------|------|-------------|
| `MessageType` | `varchar(250)` | NOT NULL, part of PRIMARY KEY |
| `InboxWorkQueueUri` | `varchar(250)` | NOT NULL, part of PRIMARY KEY |

## Key Requirements

1. `HopperOptions.Inbox.WorkTransportUri` must be set before subscription registration
2. At least one message type must be configured in `HopperOptions.Subscription.MessageTypes`
3. The `ConnectionString` option is mandatory and validated at startup
4. The `Schema` option is mandatory and must be a valid SQL identifier

## How It Works

The `SubscriptionObserver` is automatically added to the `StartupPipeline` during application initialization. When the application starts, it:

1. Creates the database schema and `SubscriberMessageType` table if `ConfigureDatabase` is `true`
2. Registers subscriptions for all configured message types based on the `SubscriptionMode`
3. Publishers use `ISubscriptionQuery.GetSubscribedUrisAsync(messageType)` to find all subscriber URIs for a given message type

