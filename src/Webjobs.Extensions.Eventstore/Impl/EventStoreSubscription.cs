using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive.Disposables;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using Microsoft.Azure.WebJobs.Host;

namespace Webjobs.Extensions.Eventstore.Impl
{
    public class EventStoreCatchUpSubscriptionObservable : IEventStoreSubscription
    {
        private EventStoreAllCatchUpSubscription _subscription;
        private readonly Lazy<IEventStoreConnection> _connection;
        private readonly UserCredentials _userCredentials;
        private readonly TraceWriter _trace;
        private Position? _lastCheckpoint;
        private readonly int _batchSize;
        private IObserver<ResolvedEvent> _observer;
        private readonly Stopwatch _catchupWatch = new Stopwatch();
        private EventBuffer _eventBuffer;

        public EventStoreCatchUpSubscriptionObservable(Lazy<IEventStoreConnection> connection,
            Position? lastCheckpoint,
            int batchSize,
            UserCredentials userCredentials,
            TraceWriter trace)
        {
            _lastCheckpoint = lastCheckpoint;
            _batchSize = batchSize;
            _userCredentials = userCredentials;
            _trace = trace;
            _connection = connection;
        }

        public IDisposable Subscribe(IObserver<ResolvedEvent> observer)
        {
            _observer = observer;
            return Disposable.Create(() => { });
        }
        
        private Position _lastAllPosition;

        public void StartCatchUpSubscription()
        {
            StartCatchUpSubscription(_lastCheckpoint);
        }

        private static readonly object LockObj = new object();
        private void StartCatchUpSubscription(Position? startPosition)
        {
            lock (LockObj)
            {
                _eventBuffer = new EventBuffer(_batchSize + 28);
            }

            var settings = new CatchUpSubscriptionSettings(100000, _batchSize, true, false);
            if (startPosition == null)
            {
                var slice =
                    _connection.Value.ReadAllEventsBackwardAsync(Position.End, 1, false, _userCredentials).Result;
                _lastAllPosition = slice.FromPosition;
            }
            _subscription = _connection.Value.SubscribeToAllFrom(
                startPosition ?? AllCheckpoint.AllStart,
                settings,
                EventAppeared,
                LiveProcessingStarted,
                SubscriptionDropped,
                _userCredentials);

            _trace.Info($"Catch-up subscription started from checkpoint {startPosition} at {DateTime.Now}.");
            _catchupWatch.Restart();
        }


        public void StopSubscription()
        {
            if (_subscription == null) return;
            try
            {
                var timeout = TimeSpan.FromSeconds(5);
                _trace.Info($"Stopping subscription with timeout {timeout}...");
                _subscription?.Stop(timeout);
                _trace.Info("Subscription stopped");
            }
            catch (TimeoutException)
            {
                _trace.Warning("The subscription did not stop within the specified time.");
            }
        }

        private void RestartSubscription()
        {
            _trace.Info("Restarting subscription...");
            var startPosition = _lastCheckpoint;
            StartCatchUpSubscription(startPosition);
        }

        private void EventAppeared(EventStoreCatchUpSubscription sub, ResolvedEvent resolvedEvent)
        {
            try
            {
                if (IsProcessable(resolvedEvent))
                {
                    _observer.OnNext(resolvedEvent);
                    var pos = resolvedEvent.OriginalPosition;
                    if (pos != null)
                    {
                        _lastCheckpoint = pos;
                    }
                }                
            }
            catch (Exception ex)
            {
                _observer.OnError(ex);
            }
        }

        private bool IsProcessable(ResolvedEvent e)
        {
            var evt = e.Event;
            if (e.OriginalStreamId.StartsWith("$")) return false;
            if (evt.EventType == "$streamDeleted") return false;
            lock (LockObj)
            {
                if (_eventBuffer != null && _eventBuffer.Contains(evt.EventId))
                {
                    _trace.Warning($"Duplicate event {evt.EventType} {evt.EventId} in stream {evt.EventNumber}@{evt.EventStreamId}. Skipping processing.");
                    return false;
                }
                _eventBuffer?.Add(evt.EventId);
            }
            return true;
        }

        private void LiveProcessingStarted(EventStoreCatchUpSubscription sub)
        {
            _observer.OnCompleted();
        }

        private void SubscriptionDropped(EventStoreCatchUpSubscription sub, SubscriptionDropReason reason, Exception e)
        {
            var msg = (e?.Message + " " + (e?.InnerException?.Message ?? "")).TrimEnd();
            _trace.Warning($"Subscription dropped because {reason}: {msg}");
            _observer.OnError(new Exception("Subscription dropped."));
        }
        
        public class EventBuffer
        {
            private readonly Queue<Guid> _buffer;
            private readonly int _maxCapacity;
            private readonly object _lock = new object();

            public EventBuffer(int maxCapacity)
            {
                _maxCapacity = maxCapacity;
                _buffer = new Queue<Guid>(maxCapacity);
            }

            public void Add(Guid eventId)
            {
                lock (_lock)
                {
                    while (_buffer.Count >= _maxCapacity)
                        _buffer.Dequeue();
                    _buffer.Enqueue(eventId);
                }
            }

            public bool Contains(Guid eventId)
            {
                lock (_lock)
                {
                    return _buffer.Contains(eventId);
                }
            }

            public void Clear()
            {
                lock (_lock)
                {
                    _buffer.Clear();
                }
            }

            public int Count => _buffer.Count;
        }
    }
}