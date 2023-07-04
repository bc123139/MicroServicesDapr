using Dapr;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NotificationApi.Commands;
using NotificationApi.Events;
using NotificationApi.Helpers;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using SixLabors.ImageSharp;
using Dapr.Client;

namespace NotificationApi.Controllers
{
    [ApiController]
    public class NotificationController : ControllerBase
    {
        private readonly DaprClient _daprClient;
        private readonly ILogger<NotificationController> _logger;

        public NotificationController(DaprClient daprClient, ILogger<NotificationController> logger)
        {
            _daprClient = daprClient;
            _logger = logger;
        }

        [Route("sendemail")]
        [HttpPost()]
        [Topic("eventbus", "OrderProcessedEvent")]
        public async Task<IActionResult> SendEmail(DispatchOrderCommand command)
        {
            _logger.LogInformation("SendEmail method entered");
            _logger.LogInformation("Order received for dispatch: " + command.OrderId);
            var metaData = new Dictionary<string, string>
            {
                ["emailFrom"] = "faceplc@abc.com",
                ["emailTo"] = "utahir604@gmail.com",
                ["subject"] = $"your order {command.OrderId}"
            };
            var rootFolder = AppContext.BaseDirectory.Substring(0, AppContext.BaseDirectory.IndexOf("bin"));
            var facesData = command.Faces;
            if (facesData.Count < 1)
            {
                _logger.LogInformation("Mo faces detected");

            }
            else
            {
                int j = 0;
                foreach (var face in facesData)
                {
                    Image img = Image.Load(face);
                    img.Save(rootFolder + "/Images/face" + j + ".jpg");
                    j++;

                }
            }
            var body = EmailUtils.CreateEmailBody(command);
            await _daprClient.InvokeBindingAsync("sendmail", "create", body, metaData);
            var eventMsg = new OrderDispatchedEvent
            {
                OrderId = command.OrderId,
                DispatchDateTime = DateTime.UtcNow
            };
            await _daprClient.PublishEventAsync<OrderDispatchedEvent>("eventbus", "OrderDispatchedEvent", eventMsg);
            _logger.LogInformation($"Order dispatched OrderId {eventMsg.OrderId} and dispatch date {eventMsg.DispatchDateTime}");
            return Ok();
        }
    }
}
