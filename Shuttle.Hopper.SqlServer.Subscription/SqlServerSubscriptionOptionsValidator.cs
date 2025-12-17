using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;

namespace Shuttle.Hopper.SqlServer.Subscription;

public class SqlServerSubscriptionOptionsValidator : IValidateOptions<SqlServerSubscriptionOptions>
{
    public ValidateOptionsResult Validate(string? name, SqlServerSubscriptionOptions options)
    {
        Guard.AgainstNull(options);

        if (string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            return ValidateOptionsResult.Fail(Resources.ConnectionStringOptionException);
        }

        if (string.IsNullOrWhiteSpace(options.Schema))
        {
            return ValidateOptionsResult.Fail(Resources.SchemaOptionException);
        }

        return ValidateOptionsResult.Success;
    }
}