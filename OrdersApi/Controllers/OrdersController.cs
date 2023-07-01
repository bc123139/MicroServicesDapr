using Dapr;
using Dapr.Client;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OrdersApi.Commands;
using OrdersApi.Persistence;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using OrdersApi.Models;
using System.Collections.Generic;
using OrdersApi.Events;

namespace OrdersApi.Controllers
{
    //[Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly ILogger<OrdersController> _logger;
        private readonly IOrderRepository _orderRepo;
        private readonly DaprClient _daprClient;

        public OrdersController(ILogger<OrdersController> logger, IOrderRepository orderRepo, DaprClient daprClient)
        {
            _logger = logger;
            _orderRepo = orderRepo;
            _daprClient = daprClient;
        }
        [Route("OrderReceived")]
        [HttpPost]
        [Topic("eventbus", "OrderReceivedEvent")]
        public async Task<IActionResult> OrderReceived(OrderReceivedCommand command)
        {
            if (command?.OrderId != null && command.PhotoUrl != null
                && command?.UserEmail != null && command?.ImageData != null)
            {
                _logger.LogInformation($"Cloud event {command.OrderId} {command.UserEmail} received");
                Image img = Image.Load(command.ImageData);
                img.Save("dummy.jpg");
                var order = new Order()
                {
                    OrderId = command.OrderId,
                    ImageData = command.ImageData,
                    UserEmail = command.UserEmail,
                    PhotoUrl = command.PhotoUrl,
                    Status = Status.Registered,
                    OrderDetails = new List<OrderDetail>()
                };
                var orderExists = await _orderRepo.GetOrderAsync(order.OrderId);
                if (orderExists == null)
                {
                    await _orderRepo.RegisterOrder(order);
                    var ore = new OrderRegisteredEvent()
                    {
                        OrderId = order.OrderId,
                        UserEmail = order.UserEmail,
                        ImageData = order.ImageData
                    };
                    await _daprClient.PublishEventAsync("eventbus", "OrderRegisteredEvent", ore);
                    _logger.LogInformation($"For {order.OrderId}, OrderRegisteredEvent published");
                }
                return Ok();
            }
                return BadRequest();
        }


        [Route("orderprocessed")]
        [HttpPost()]
        [Topic("eventbus", "OrderProcessedEvent")]
        public async Task<IActionResult> OrderProcessed(OrderStatusChangedToProcessedCommand command)
        {
            _logger.LogInformation("OrderProcessed method entered");
            if (ModelState.IsValid)
            {
                Order order = await _orderRepo.GetOrderAsync(command.OrderId);
                if (order != null)
                {
                    order.Status = Status.Processed;
                    int j = 1;
                    foreach (var face in command.Faces)
                    {
                        Image img = Image.Load(face);
                        img.Save("face" + j + ".jpg");
                        j++;
                        var orderDetail = new OrderDetail
                        {
                            OrderId = order.OrderId,
                            FaceData = face
                        };
                        order.OrderDetails.Add(orderDetail);
                    }

                    await _orderRepo.UpdateOrder(order);
                }
            }
            return Ok();
        }
    }
}
