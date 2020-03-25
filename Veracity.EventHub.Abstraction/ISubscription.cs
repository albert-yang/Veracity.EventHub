using System;
using System.Collections.Generic;
using System.Text;

namespace Veracity.EventHub.Abstraction
{
    public interface ISubscription
    {
        string Namespace { get; }

        string EventType { get; }

        void Unsubscribe();
    }
}
