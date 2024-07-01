using MassTransit;
using MassTransitBatchRetryIssue;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<MyDbContext>(optionsBuilder => optionsBuilder.UseSqlServer(builder.Configuration.GetConnectionString("db")));

builder.Services.AddMassTransit(x =>
{
    x.AddConsumers(typeof(Program).Assembly);

    x.AddEntityFrameworkOutbox<MyDbContext>(o =>
    {
        o.DisableInboxCleanupService();
        o.UseSqlServer();
        o.UseBusOutbox();
    });
    
    x.UsingAzureServiceBus((context,cfg) =>
    {
        cfg.Host(builder.Configuration.GetConnectionString("sb"));
        cfg.ConfigureEndpoints(context);
    });
        
    x.AddConfigureEndpointsCallback((context, _, configurator) =>
    {
        if (configurator is IServiceBusReceiveEndpointConfigurator sb)
        {
            // Opt for Azure Service Bus DLQ over MT _error and fault queues
            sb.PublishFaults = false;
            sb.ConfigureDeadLetterQueueErrorTransport();
            sb.ConfigureDeadLetterQueueDeadLetterTransport(); 
            sb.UseEntityFrameworkOutbox<MyDbContext>(context);
        }
    });
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();


app.MapGet("/send", async (IPublishEndpoint publish, MyDbContext context) =>
{
    await publish.Publish(new DummyEvent
    {
        Id = Guid.NewGuid()
    });

    await context.SaveChangesAsync();
});

app.Run();



