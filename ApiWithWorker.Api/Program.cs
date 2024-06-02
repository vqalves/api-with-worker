
using RabbitMQ.Client;

namespace ApiWithWorker.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllers();

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Register RabbitMQ services
            builder.Services.AddSingleton<ConnectionFactory>(p =>
            {
                var logger = p.GetRequiredService<ILogger<ConnectionFactory>>();

                var rabbitConnectionString = Environment.GetEnvironmentVariable("RABBIT_CONNECTION_STRING");
                var rabbitConnectionUri = new Uri(rabbitConnectionString);

                logger.LogInformation("Setting Rabbit HostName as '{rabbitHostName}'", rabbitConnectionUri.Authority);

                return new ConnectionFactory()
                {
                    Uri = rabbitConnectionUri,
                    DispatchConsumersAsync = true,
                    ConsumerDispatchConcurrency = 200
                };
            });

            builder.Services.AddSingleton<IConnection>(p =>
            {
                var factory = p.GetRequiredService<ConnectionFactory>();
                return factory.CreateConnection();
            });

            // Register workers
            builder.Services.AddHostedService<ExampleWorkerService>();

            var app = builder.Build();

            // RabbitMQ setup
            {
                var connection = app.Services.GetRequiredService<IConnection>();
                using var channel = connection.CreateModel();

                channel.ExchangeDeclare
                (
                    exchange: "api-with-worker-exchange",
                    type: "fanout",
                    durable: true,
                    autoDelete: false,
                    arguments: null
                );

                channel.QueueDeclare
                (
                    queue: "api-with-worker-queue",
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null
                );

                channel.QueueBind
                (
                    queue: "api-with-worker-queue",
                    exchange: "api-with-worker-exchange",
                    routingKey: "#",
                    arguments: null
                );
            }

            app.UseSwagger();
            app.UseSwaggerUI();
            app.UseHttpsRedirection();
            app.MapControllers();
            app.Run();
        }
    }
}
