//using System;
//using Azure.Messaging.EventHubs;
//using Microsoft.Azure.Functions.Worker;
//using Microsoft.Extensions.Logging;

//namespace OrderPipeline.Functions
//{
//    public class ProcessPayment
//    {
//        private readonly ILogger<ProcessPayment> _logger;

//        public ProcessPayment(ILogger<ProcessPayment> logger)
//        {
//            _logger = logger;
//        }

//        [Function(nameof(ProcessPayment))]
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
    public class ProcessPayment
    {
        private readonly ILogger _logger;
        private readonly EventHubProducerClient _producer;

        public ProcessPayment(ILoggerFactory loggerFactory, IConfiguration config)
        {
            _logger = loggerFactory.CreateLogger<ProcessPayment>();
            _producer = new EventHubProducerClient(
                config["EventHubConnection"], "orderhub");
        }

        [Function("ProcessPayment")]
        public async Task Run([EventHubTrigger("orderhub", Connection = "EventHubConnection")] string[] events)
        {
            foreach (var evt in events)
            {
                var order = JsonSerializer.Deserialize<Order>(evt);

                if (order.Stage != "ProcessPayment")
                    continue;

                // Fake payment processing
                order.Status = "PaymentSuccessful";
                order.Stage = "SubmitToFulfillment";

                using var batch = await _producer.CreateBatchAsync();
                batch.TryAdd(new EventData(JsonSerializer.SerializeToUtf8Bytes(order))
                {
                    Properties = { { "Stage", "SubmitToFulfillment" } }
                });
                await _producer.SendAsync(batch);

                _logger.LogInformation($"Payment processed for {order.OrderId}.");
            }
        }
    }
}
