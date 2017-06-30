using EventStore.ClientAPI;

namespace Webjobs.Extensions.Eventstore.Impl
{
    internal class LiveProcessingStartedTriggerValue
    {
        public EventStoreCatchUpSubscription Subscription { get; }

        public LiveProcessingStartedTriggerValue(EventStoreCatchUpSubscription subscription)
        {
            Subscription = subscription;
        }
    }
}