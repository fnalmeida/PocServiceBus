using System.Diagnostics.Eventing.Reader;
using System.Net.NetworkInformation;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var session = builder.Configuration.GetSection("SERVICEBUS");
var connectionString = session["NAMESPACE-CONNECTION-STRING"];
var queue = session["QUEUE-NAME"];
builder.Services.AddScoped<ServiceBusConnection>(dao => new ServiceBusConnection(connectionString, queue));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();


 app.MapPost("/receiverHook", async (dynamic payload, ILogger<Program> logger, ServiceBusConnection con) =>
{
    logger.LogInformation($"payload recebido : {payload}");

    if (await MessageClient.senderMessage(payload, con))
       return Results.Ok(payload);
    else
      return  Results.BadRequest("Mensagem não processada");
    
})
    .Produces(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status400BadRequest)
    .WithName("receiverHook")
    .WithOpenApi();

app.Run();


internal class MessageClient
{

    public static async Task<bool> senderMessage(dynamic message, ServiceBusConnection con)
    {
        ServiceBusClient client;
        ServiceBusSender sender;

        var clientOptions = new ServiceBusClientOptions()
        {
            TransportType = ServiceBusTransportType.AmqpWebSockets
        };
        client = new ServiceBusClient(con.ConnectionString, clientOptions);
        sender = client.CreateSender(con.Queue);

        using ServiceBusMessageBatch messageBatch = await sender.CreateMessageBatchAsync();
        messageBatch.TryAddMessage(new ServiceBusMessage(JsonSerializer.Serialize(message)));

        try
        {
            await sender.SendMessagesAsync(messageBatch);
            Console.WriteLine($"messages has been published to the queue.");
        }
        finally
        {
            await sender.DisposeAsync();
            await client.DisposeAsync();

        }

        return true;
    }
}

internal class ServiceBusConnection(string? ConnectionString, string? Queue)
{
    public string? ConnectionString { get; } = ConnectionString;
    public string? Queue { get; } = Queue;
}


internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
