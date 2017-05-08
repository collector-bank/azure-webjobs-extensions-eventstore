using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;

namespace Webjobs.Extensions.Eventstore.Impl
{
    public class EventStoreListener : IListener
    {
        private readonly ITriggeredFunctionExecutor _executor;
        private readonly IEventStoreSubscription _eventStoreSubscription;
        private readonly ILiveProcessingReached _liveProcessingReached;
        private readonly TraceWriter _trace;
        private CancellationToken _cancellationToken = CancellationToken.None;
        private IDisposable _observable;

        public int TimeOutInMilliSeconds { get; set; }
        public int BatchSize { get; set; }


        public EventStoreListener(ITriggeredFunctionExecutor executor, 
                                  IEventStoreSubscription eventStoreSubscription,
                                  ILiveProcessingReached liveProcessingReached,
                                  TraceWriter trace)
        {
            _executor = executor;
            _eventStoreSubscription = eventStoreSubscription;
            _liveProcessingReached = liveProcessingReached;
            _trace = trace;
            _observable = _eventStoreSubscription
                          .Buffer(TimeSpan.FromMilliseconds(500), 100)
                          .Where(buffer => buffer.Any())
                          .Subscribe(ProcessEvent, OnCompleted);
        }
        
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
            _eventStoreSubscription.StartCatchUpSubscription();
            return Task.FromResult(true);
        }

        private void OnCompleted()
        {
            _trace.Info("Subscription catch up complete calling handler");
            _liveProcessingReached?.Handle();
            _observable = _eventStoreSubscription
                          .Buffer(TimeSpan.FromMilliseconds(TimeOutInMilliSeconds), BatchSize)
                          .Where(buffer => buffer.Any())
                          .Subscribe(ProcessEvent);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _observable.Dispose();
            return Task.FromResult(true);
        }

        private void ProcessEvent(IEnumerable<ResolvedEvent> events)
        {
            _trace.Info($"Processing message triggered with {events.Count()} events");
            TriggeredFunctionData input = new TriggeredFunctionData
            {
                TriggerValue = new EventStoreTriggerValue(events)
            };
            _executor.TryExecuteAsync(input, _cancellationToken).Wait();
        }
        
        public void Cancel()
        {
            _observable.Dispose();
        }

        public void Dispose()
        {
            _observable.Dispose();
        }
    }
}