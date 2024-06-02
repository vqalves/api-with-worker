using Microsoft.AspNetCore.Mvc;
using RabbitMQ.Client;
using System.Text;

namespace ApiWithWorker.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MessageController : ControllerBase
    {
        private readonly ILogger<MessageController> Logger;

        public MessageController(ILogger<MessageController> logger)
        {
            Logger = logger;
        }

        [HttpGet]
        [Route("send")]
        public async Task<IResult> Send([FromServices] IConnection connection, [FromQuery] string content)
        {
            var body = Encoding.UTF8.GetBytes(content);

            using var channel = connection.CreateModel();

            channel.BasicPublish
            (
                exchange: "api-with-worker-exchange",
                routingKey: string.Empty,
                basicProperties: null,
                body: body
            );

            Logger.LogInformation("Enqueued message: {message}", content);

            return Results.Ok();
        }

        [HttpGet]
        [Route("send_batch")]
        public async Task<IResult> BatchSend([FromServices] IConnection connection, [FromQuery] string content, [FromQuery] int count, [FromQuery] int batchSize)
        {
            using var channel = connection.CreateModel();

            var properties = channel.CreateBasicProperties();
            properties.Persistent = true;

            var sentMessagesCounter = 0;
            while (count > 0)
            {
                var batch = channel.CreateBasicPublishBatch();

                var currentLoop = count < batchSize ? count : batchSize;
                count -= currentLoop;

                for (var i = 1; i <= currentLoop; i++)
                {
                    sentMessagesCounter++;
                    var body = Encoding.UTF8.GetBytes($"{content} ({sentMessagesCounter})");
                    var readOnly = new ReadOnlyMemory<byte>(body);

                    batch.Add
                    (
                        exchange: "api-with-worker-exchange",
                        routingKey: string.Empty,
                        mandatory: true,
                        properties: properties,
                        body: readOnly
                    );
                }

                batch.Publish();

                Logger.LogInformation
                (
                    "Enqueued batch messages: {message} (count: {count}, remaining {remaining})", 
                    content, 
                    currentLoop, 
                    count
                );
            }

            return Results.Ok();
        }
    }
}
