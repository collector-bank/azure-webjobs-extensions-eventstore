using EventStore.ClientAPI;

namespace Webjobs.Extensions.Eventstore.Impl
{
    public class LiveProcessingStartedContext
    {
        public EventStoreCatchUpSubscription Subscription { get; }
        public LiveProcessingStartedContext(EventStoreCatchUpSubscription subscription)
        {
            Subscription = subscription;
        }
    }
}