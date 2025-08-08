//using System;
//using Azure.Messaging.EventHubs;
//using Microsoft.Azure.Functions.Worker;
//using Microsoft.Extensions.Logging;

//namespace OrderPipeline.Functions
//{
//    public class ApplyDiscounts
//    {
//        private readonly ILogger<ApplyDiscounts> _logger;

//        public ApplyDiscounts(ILogger<ApplyDiscounts> logger)
//        {
//            _logger = logger;
//        }

//        [Function(nameof(ApplyDiscounts))]
//        public void Run([EventHubTrigger("samples-workitems", Connection = "")] EventData[] events)
//        {
//            foreach (EventData @event in events)
//            {
//                _logger.LogInformation("Event Body: {body}", @event.Body);
//                _logger.LogInformation("Event Content-Type: {contentType}", @event.ContentType);
//            }
//        }
//    }
//}

using System.Text.Json;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OrderPipeline.Models;

namespace OrderPipeline.Functions
{
    public class ApplyDiscounts
    {
        private readonly ILogger _logger;
        private readonly EventHubProducerClient _producer;

        public ApplyDiscounts(ILoggerFactory loggerFactory, IConfiguration config)
        {
            _logger = loggerFactory.CreateLogger<ApplyDiscounts>();
            _producer = new EventHubProducerClient(
                config["EventHubConnection"], "orderhub");
        }

        [Function("ApplyDiscounts")]
        public async Task Run([EventHubTrigger("orderhub", Connection = "EventHubConnection")] string[] events)
        {
            foreach (var evt in events)
            {
                var order = JsonSerializer.Deserialize<Order>(evt);

                if (order.Stage != "ApplyDiscounts")
                    continue;

                // Example: 10% discount
                order.Discount = order.Amount * 0.10m;
                order.Amount -= order.Discount;
                order.Stage = "ProcessPayment";

                using var batch = await _producer.CreateBatchAsync();
                batch.TryAdd(new EventData(JsonSerializer.SerializeToUtf8Bytes(order))
                {
                    Properties = { { "Stage", "ProcessPayment" } }
                });
                await _producer.SendAsync(batch);

                _logger.LogInformation($"Applied discount to {order.OrderId}.");
            }
        }
    }
}
