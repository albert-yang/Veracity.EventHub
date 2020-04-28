using System;
using System.Collections.Generic;
using System.Text;

namespace Veracity.EventHub.Abstraction
{
    public class EventMessage
    {
        public string Namespace { get; set; }
        
        public string RouteKey { get; set; }

        public string EventType { get; set; }
        
        public string ContentType { get; set; }

        public string ContentEncoding { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public byte[] MessageBody { get; set; }
    }
}
