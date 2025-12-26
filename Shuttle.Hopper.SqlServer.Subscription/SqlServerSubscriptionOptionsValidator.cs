using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;

namespace Shuttle.Hopper.SqlServer.Subscription;

public class SqlServerSubscriptionOptionsValidator : IValidateOptions<SqlServerSubscriptionOptions>
{
    public ValidateOptionsResult Validate(string? name, SqlServerSubscriptionOptions options)
    {
        Guard.AgainstNull(options);

        if (string.IsNullOrWhiteSpace(options.ConnectionStringName))
        {
            return ValidateOptionsResult.Fail(Resources.ConnectionStringNameOptionException);
        }

        if (string.IsNullOrWhiteSpace(options.Schema))
        {
            return ValidateOptionsResult.Fail(Resources.SchemaOptionException);
        }

        if (!System.Text.RegularExpressions.Regex.IsMatch(options.Schema, "^[a-zA-Z_][a-zA-Z0-9_]*$"))
        {
            return ValidateOptionsResult.Fail(Resources.SchemaIdentifierException);
        }

        return ValidateOptionsResult.Success;
    }
}