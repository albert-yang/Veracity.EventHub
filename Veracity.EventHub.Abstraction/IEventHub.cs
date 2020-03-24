using System;
using System.Threading.Tasks;

namespace Veracity.EventHub.Abstraction
{
    public interface IEventHub
    {
        void Subscribe(string @namespace, string eventType, Func<EventMessage, Task> handler);

        void Publish(EventMessage eventMessage);
    }
}
