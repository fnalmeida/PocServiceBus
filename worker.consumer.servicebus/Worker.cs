using Azure.Messaging.ServiceBus;

namespace worker.consumer.servicebus
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly ServiceBusConnection _con;
        private ServiceBusProcessor _serviceBusProcessor;

        public Worker(ILogger<Worker> logger, ServiceBusConnection con)
        {
            _logger = logger;
            _con = con;

            var clientOptions = new ServiceBusClientOptions()
            {
                TransportType = ServiceBusTransportType.AmqpWebSockets
            };
            var client = new ServiceBusClient(_con.ConnectionString, clientOptions);
            _serviceBusProcessor = client.CreateProcessor(_con.Queue, new ServiceBusProcessorOptions());
            _serviceBusProcessor.ProcessMessageAsync += ProcessMessageAsync;
            _serviceBusProcessor.ProcessErrorAsync += ErrorHandler;
        }
       
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _serviceBusProcessor.StartProcessingAsync(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                if (_logger.IsEnabled(LogLevel.Information))
                {
                   // _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);                  
                }
                await Task.Delay(1000, stoppingToken);
            }
        }

        private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
        {
            try
            {
                // Your message processing logic here
                var messageBody = args.Message.Body.ToString();
                _logger.LogInformation($"Received message: {messageBody}");

                // Complete the message to remove it from the queue
                await args.CompleteMessageAsync(args.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message");
                // Handle exceptions (e.g., dead-letter, retry, etc.)
            }
        }
        Task ErrorHandler(ProcessErrorEventArgs args)
        {
            Console.WriteLine(args.Exception.ToString());
            return Task.CompletedTask;
        }
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await _serviceBusProcessor.StopProcessingAsync();
            await base.StopAsync(cancellationToken);
        }
    }
}
