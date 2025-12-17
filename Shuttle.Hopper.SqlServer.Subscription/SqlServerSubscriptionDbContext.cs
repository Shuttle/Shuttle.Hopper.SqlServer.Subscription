using Microsoft.EntityFrameworkCore;
using Shuttle.Core.Contract;

namespace Shuttle.Hopper.SqlServer.Subscription;

public class SqlServerSubscriptionDbContext(DbContextOptions<SqlServerSubscriptionDbContext> options, string schema) : DbContext(options)
{
    public string Schema => Guard.AgainstEmpty(schema);

    public DbSet<SubscriberMessageType> SubscriberMessageTypes { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        base.OnModelCreating(modelBuilder);
    }
}