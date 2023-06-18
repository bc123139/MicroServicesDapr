using System.Collections.Generic;
using System;

namespace FacesApi.Events
{
    public class OrderProcessedEvent
    {
        public Guid OrderId { get; set; }
        public string UserEmail { get; set; }
        public byte[] ImageData { get; set; }
        public List<byte[]> Faces { get; set; }
    }
}
