using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;

namespace Webjobs.Extensions.Eventstore.Impl
{
    public class LiveProcessingStartedListener : IListener
    {
        private readonly ITriggeredFunctionExecutor _executor;
        private IEventStoreSubscription _eventStoreSubscription;
        private readonly TraceWriter _trace;
        private CancellationToken _cancellationToken = CancellationToken.None;
        private IDisposable _observable;
        
        public LiveProcessingStartedListener(ITriggeredFunctionExecutor executor,
            IEventStoreSubscription eventStoreSubscription,
            TraceWriter trace)
        {
            _executor = executor;
            _eventStoreSubscription = eventStoreSubscription;
            _trace = trace;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
            _observable = _eventStoreSubscription.Subscribe(x => {}, OnCompleted);
            _eventStoreSubscription.Start(cancellationToken, 200);

            return Task.FromResult(true);
        }

        private void OnCompleted()
        {
            var input = new TriggeredFunctionData
            {
                TriggerValue = new LiveProcessingStartedTriggerValue(null)
            };
            _executor.TryExecuteAsync(input, _cancellationToken).Wait();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Cancel();
            return Task.FromResult(true);
        }

        public void Cancel()
        {
            _observable?.Dispose();
            _eventStoreSubscription?.Stop();
        }

        private bool _isDisposed;
        public void Dispose()
        {
            if (!_isDisposed)
            {
                Dispose(true);
            }
            _isDisposed = true;
        }

        private void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                Cancel();
                _eventStoreSubscription = null;
            }
            GC.SuppressFinalize(this);
        }
    }
}