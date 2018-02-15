using Microsoft.Azure.WebJobs.Host;

namespace Webjobs.Extensions.Eventstore
{
    public interface IEventStoreSubscriptionFactory
    {
        IEventStoreSubscription Create(EventStoreConfig eventStoreConfig, TraceWriter traceWriter,string stream = null);
    }
}