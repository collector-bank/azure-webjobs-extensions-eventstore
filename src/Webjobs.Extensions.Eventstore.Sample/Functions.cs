using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using Microsoft.Azure.WebJobs;

namespace Webjobs.Extensions.Eventstore.Sample
{
    public class Functions
    {
        private readonly IEventPublisher<ResolvedEvent> _eventPublisher;
        private const string WebJobDisabledSetting = "WebJob_Disabled";

        public Functions(IEventPublisher<ResolvedEvent> eventPublisher)
        {
            _eventPublisher = eventPublisher;
        }

        [Disable(WebJobDisabledSetting)]
        [Singleton(Mode = SingletonMode.Listener)]
        public void ProcessQueueMessage([EventTrigger(BatchSize = 10, TimeOutInMilliSeconds = 20)] IEnumerable<ResolvedEvent> events)
        {
            foreach (var evt in events)
            {
               _eventPublisher.Publish(evt);
            }
        }
    }
}
