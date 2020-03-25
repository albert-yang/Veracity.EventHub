using System;
using System.Collections.Generic;
using System.Text;

namespace Veracity.EventHub.Abstraction
{
    public class EventHeader
    {
        public string Namespace { get; set; }

        public string EventType { get; set; }

        public string RequestId { get; set; }

        public IDictionary<string, string> CorrelationContext { get; set; }
    }

    public class EventMessage
    {
        public EventHeader Header { get; set; }

        public byte[] MessageBody { get; set; }
    }
}
