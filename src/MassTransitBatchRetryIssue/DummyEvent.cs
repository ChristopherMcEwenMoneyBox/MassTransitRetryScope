using MassTransit;

namespace MassTransitBatchRetryIssue;


public class DummyEvent
{
    public Guid Id { get; set; }
}

public class DummyEventConsumer(MyDbContext myDbContext) : IConsumer<Batch<DummyEvent>>
{
    public Task Consume(ConsumeContext<Batch<DummyEvent>> context)
    {
        var trackedChanges = myDbContext.ChangeTracker.HasChanges();

        if (trackedChanges)
        {
            throw new Exception("Existing scoped reused 😓");
        }
        
        myDbContext.DummyModels.Add(new DummyModel { Id = Guid.NewGuid() });
        
        throw new Exception("Fake exception to cause retry");
        
        return Task.CompletedTask;
    }
}

public class DummyEventConsumerDefinition : ConsumerDefinition<DummyEventConsumer>
{
    protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator, IConsumerConfigurator<DummyEventConsumer> consumerConfigurator, IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(r => r.Interval(5, 1000));
        endpointConfigurator.PrefetchCount = 50; 
        
        consumerConfigurator.Options<BatchOptions>(options => options
            .SetMessageLimit(50)
            .SetTimeLimit(1000)
            .SetConcurrencyLimit(10));
    }
}