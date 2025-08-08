
#region "Default Code Function PlaceOrder"
//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.Azure.Functions.Worker;
//using Microsoft.Extensions.Logging;

//namespace OrderPipeline.Functions
//{
//    public class PlaceOrder
//    {
//        private readonly ILogger<PlaceOrder> _logger;

//        public PlaceOrder(ILogger<PlaceOrder> logger)
//        {
//            _logger = logger;
//        }

//        [Function("PlaceOrder")]
//        public IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
//        {
//            _logger.LogInformation("C# HTTP trigger function processed a request.");
//            return new OkObjectResult("Welcome to Azure Functions!");
//        }
//    }
//}

#endregion

using System.Net;
using System.Text.Json;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OrderPipeline.Models;

namespace OrderPipeline.Functions
{
    public class PlaceOrder
    {
        private readonly ILogger _logger;
        private readonly EventHubProducerClient _producer;

        public PlaceOrder(ILoggerFactory loggerFactory, IConfiguration config)
        {
            _logger = loggerFactory.CreateLogger<PlaceOrder>();
            _producer = new EventHubProducerClient(
                config["EventHubConnection"], "orderhub");
        }

        [Function("PlaceOrder")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
        {
            var order = await JsonSerializer.DeserializeAsync<Order>(req.Body);
            order.Stage = "ValidateOrder";
            order.Status = "New";

            using var batch = await _producer.CreateBatchAsync();
            batch.TryAdd(new EventData(JsonSerializer.SerializeToUtf8Bytes(order))
            {
                Properties = { { "Stage", "ValidateOrder" } }
            });

            await _producer.SendAsync(batch);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync($"Order {order.OrderId} placed.");
            return response;
        }
    }
}
