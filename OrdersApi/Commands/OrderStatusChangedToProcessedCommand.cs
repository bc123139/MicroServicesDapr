using System.Collections.Generic;
using System;

namespace OrdersApi.Commands
{
    public class OrderStatusChangedToProcessedCommand
    {
        public Guid OrderId { get; set; }
        public List<byte[]> Faces { get; set; }
    }
}
