using worker.consumer.servicebus;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

var session = builder.Configuration.GetSection("SERVICEBUS");
var connectionString = session["NamespaceConnectionString"];
var queue = session["QueueName"];

builder.Services.AddSingleton<ServiceBusConnection>(x => new ServiceBusConnection(connectionString, queue));

var host = builder.Build();
host.Run();

public class ServiceBusConnection(string? ConnectionString, string? Queue)
{
    public string? ConnectionString { get; } = ConnectionString;
    public string? Queue { get; } = Queue;
}