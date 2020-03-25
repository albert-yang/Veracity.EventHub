using System;
using System.Threading.Tasks;

namespace Veracity.EventHub.Abstraction
{
    public interface IEventHub
    {
        ISubscription Subscribe(string @namespace, string filter, Func<EventMessage, Task> handler);

        ISubscription Subscribe(string @namespace, Func<EventMessage, Task> handler);

        void Publish(EventMessage eventMessage);
    }
}
