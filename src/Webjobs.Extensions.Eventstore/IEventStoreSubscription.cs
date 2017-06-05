using System;
using System.Threading;
using System.Threading.Tasks;
using EventStore.ClientAPI;

namespace Webjobs.Extensions.Eventstore
{
    public interface IEventStoreSubscription : IObservable<ResolvedEvent>
    {
        void Start(CancellationToken token);
        void Restart();
        void Stop();
    }
}