using System;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using EventStore.ClientAPI;

namespace Webjobs.Extensions.Eventstore
{
    public interface IEventStoreSubscription : IConnectableObservable<ResolvedEvent>
    {
        void Start(CancellationToken token, int batchSize = 200);
        void Restart();
        void Stop();
    }
}