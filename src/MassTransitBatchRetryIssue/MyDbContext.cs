using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace MassTransitBatchRetryIssue;

public class MyDbContext(DbContextOptions<MyDbContext> options) : DbContext(options)
{
    public DbSet<DummyModel> DummyModels { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }
}

public class DummyModel
{
    public Guid Id { get; set; }
}