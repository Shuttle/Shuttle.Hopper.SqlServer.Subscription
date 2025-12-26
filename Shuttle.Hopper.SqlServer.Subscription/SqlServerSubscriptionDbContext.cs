using Microsoft.EntityFrameworkCore;

namespace Shuttle.Hopper.SqlServer.Subscription;

public class SqlServerSubscriptionDbContext(DbContextOptions<SqlServerSubscriptionDbContext> options) : DbContext(options);