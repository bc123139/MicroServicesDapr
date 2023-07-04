using System.Collections.Generic;
using System;

namespace NotificationApi.Commands
{
    public class DispatchOrderCommand
    {

        public Guid OrderId { get; set; }
        public string UserEmail { get; set; }

        public byte[] ImageData { get; set; }
        public List<byte[]> Faces { get; set; }
    }
}
