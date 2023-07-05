using System;

namespace OrdersApi.Events
{
    public class OrderStatusChangedToDispatched
    {
        public Guid OrderId { get; set; }
        public DateTime DispatchDateTime { get; set; }

    }
}
