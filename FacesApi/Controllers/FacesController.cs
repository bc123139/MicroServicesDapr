using Dapr;
using Dapr.Client;
using FacesApi.Commands;
using FacesApi.Events;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FacesApi.Controllers
{
    [ApiController]
    public class FacesController : ControllerBase
    {
        private readonly ILogger<FacesController> _logger;
        private readonly DaprClient _daprClient;

        public FacesController(ILogger<FacesController> logger, DaprClient daprClient)
        {
            _logger = logger;
            _daprClient = daprClient;
        }

        [Route("processorder")]
        [HttpPost]
        [Topic("eventbus", "OrderRegisteredEvent")]
        public async Task<IActionResult> ProcessOrder([FromBody] ProcessOrderCommand command)
        {
            _logger.LogInformation("ProcessOrder method entered....");
            if (ModelState.IsValid)
            {
                _logger.LogInformation($"Command params: {command.OrderId}");
                Image img = Image.Load(command.ImageData);
                img.Save("dummy.jpg");
                var orderState = await _daprClient.GetStateEntryAsync<List<ProcessOrderCommand>>("redisstore", "orderList");
                List<ProcessOrderCommand> orderList = new();
                if (orderState.Value == null)
                {
                    _logger.LogInformation("OrderState Case 1 ");
                    orderList.Add(command);
                    await _daprClient.SaveStateAsync("redisstore", "orderList", orderList);
                }
                else
                {
                    _logger.LogInformation("OrderState  Case 2 ");
                    orderList = orderState.Value;
                    orderList.Add(command);
                    await _daprClient.SaveStateAsync("redisstore", "orderList", orderList);
                }
            }
            return Ok();
        }

        [HttpPost("cron")]
        public async Task<IActionResult> Cron()
        {
            _logger.LogInformation("Cron method entered");
            var orderState = await _daprClient.GetStateEntryAsync<List<ProcessOrderCommand>>("redisstore", "orderList");
            if (orderState?.Value?.Count > 0)
            {
                _logger.LogInformation($"Count value of the orders in the store {orderState.Value.Count}");
                var orderList = orderState.Value;
                var firstInTheList = orderList[0];
                if (firstInTheList != null)
                {
                    _logger.LogInformation($"First order's OrderId : {firstInTheList.OrderId}");
                    byte[] imageBytes = firstInTheList.ImageData.ToArray();
                    Image img = Image.Load(imageBytes);
                    img.Save("dummy1.jpg");
                    List<byte[]> facesCropped = await UploadPhotoAndDetectFaces(img, new MemoryStream(imageBytes));
                    var ope = new OrderProcessedEvent()
                    {
                        OrderId = firstInTheList.OrderId,
                        UserEmail = firstInTheList.UserEmail,
                        ImageData = firstInTheList.ImageData,
                        Faces = facesCropped

                    };
                    await _daprClient.PublishEventAsync<OrderProcessedEvent>("eventbus", "OrderProcessedEvent", ope);
                    orderList.Remove(firstInTheList);
                    await _daprClient.SaveStateAsync("redisstore", "orderList", orderList);
                    _logger.LogInformation($"Order List count after processing  {orderList.Count}");
                    return new OkResult();
                }
            }
            return NoContent();
        }

        private async Task<List<byte[]>> UploadPhotoAndDetectFaces(Image img, MemoryStream imageStream)
        {
            var faceList = new List<byte[]>();
            return faceList;
        }
    }
}
