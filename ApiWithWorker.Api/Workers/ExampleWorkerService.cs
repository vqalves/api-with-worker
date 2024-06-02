using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

public class ExampleWorkerService : BackgroundService
{
    private readonly ILogger<ExampleWorkerService> Logger;
    private readonly ConnectionFactory ConnectionFactory;

    public ExampleWorkerService(
        ILogger<ExampleWorkerService> logger,
        ConnectionFactory connectionFactory)
    {
        Logger = logger;
        ConnectionFactory = connectionFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var connection = ConnectionFactory.CreateConnection();
        using var channel = connection.CreateModel();
        channel.BasicQos(0, (ushort)ConnectionFactory.ConsumerDispatchConcurrency, false);

        object locker = new object();
        int count = 0;

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.Received += async (model, ea) =>
        {
            var data = Encoding.UTF8.GetString(ea.Body.ToArray());

            lock (locker)
            {
                count++;

                if (count == 1_000)
                {
                    Logger.LogInformation("Received message: {message}", data);
                    count = 0;
                }
            }

            channel.BasicAck
            (
                deliveryTag: ea.DeliveryTag, 
                multiple: false
            );

            await Task.CompletedTask;
        };

        channel.BasicConsume(queue: "api-with-worker-queue", autoAck: false, consumer: consumer);

        Logger.LogInformation("Worker starting listening to queue {queueName}", "api-with-worker-queue");

        var delay = TimeSpan.FromSeconds(5);
        while (!stoppingToken.IsCancellationRequested)
            await Task.Delay(delay);
    }


    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        Logger.LogInformation("Worker is stopping.");

        await base.StopAsync(stoppingToken);
    }
}