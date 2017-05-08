using System;
using EventStore.ClientAPI;

namespace Webjobs.Extensions.Eventstore
{
    public interface IEventStoreSubscription : IObservable<ResolvedEvent>
    {
        void StartCatchUpSubscription();
        void StopSubscription();
    }
}