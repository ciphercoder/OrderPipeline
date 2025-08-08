#region old code
//using System;
//using Azure.Messaging.EventHubs;
//using Microsoft.Azure.Functions.Worker;
//using Microsoft.Extensions.Logging;

//namespace OrderPipeline.Functions
//{
//    public class ValidateOrder
//    {
//        private readonly ILogger<ValidateOrder> _logger;

//        public ValidateOrder(ILogger<ValidateOrder> logger)
//        {
//            _logger = logger;
//        }

//        [Function(nameof(ValidateOrder))]
//        public void Run([EventHubTrigger("orderhub", Connection = "EventHubConnection")] EventData[] events)
//        {
//            foreach (EventData @event in events)
//            {
//                _logger.LogInformation("Event Body: {body}", @event.Body);
//                _logger.LogInformation("Event Content-Type: {contentType}", @event.ContentType);
//            }
//        }
//    }
//}

#endregion


using System.Text.Json;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OrderPipeline.Models;

namespace OrderPipeline.Functions
{
    public class ValidateOrder
    {
        private readonly ILogger _logger;
        private readonly EventHubProducerClient _producer;

        public ValidateOrder(ILoggerFactory loggerFactory, IConfiguration config)
        {
            _logger = loggerFactory.CreateLogger<ValidateOrder>();
            _producer = new EventHubProducerClient(
                config["EventHubConnection"], "orderhub");
        }

        [Function("ValidateOrder")]
        public async Task Run([EventHubTrigger("orderhub", Connection = "EventHubConnection")] string[] events)
        {
            foreach (var evt in events)
            {
                var order = JsonSerializer.Deserialize<Order>(evt);

                if (order.Stage != "ValidateOrder")
                    continue;

                // Simple validation
                order.Status = order.Amount > 0 ? "Validated" : "Invalid";
                order.Stage = "ApplyDiscounts";

                using var batch = await _producer.CreateBatchAsync();
                batch.TryAdd(new EventData(JsonSerializer.SerializeToUtf8Bytes(order))
                {
                    Properties = { { "Stage", "ApplyDiscounts" } }
                });
                await _producer.SendAsync(batch);

                _logger.LogInformation($"Order {order.OrderId} validated.");
            }
        }
    }
}

