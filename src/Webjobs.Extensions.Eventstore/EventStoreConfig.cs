using System;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Webjobs.Extensions.Eventstore.Impl;

namespace Webjobs.Extensions.Eventstore
{
    /// <summary>
    /// The configuration used to setup the event store subscription and 
    /// binding process.
    /// </summary>
    public class EventStoreConfig : IExtensionConfigProvider
    {
        /// <summary>
        /// Factory that creates a connection object to event store.
        /// </summary>
        public IEventStoreConnectionFactory EventStoreConnectionFactory { get; set; }
        /// <summary>
        /// Factory used to create user credentials for event store subscription.
        /// </summary>
        public IUserCredentialFactory UserCredentialFactory { get; set; }

        /// <summary>
        /// The position in the stream for the last event processed.
        /// If not position is supplied, the subscription will start from 
        /// the beginning.
        /// </summary>
        public Position? LastPosition { get; set; }

        /// <summary>
        /// Factory used to create an event store listener.
        /// </summary>
        public IListenerFactory EventStoreListenerFactory { get; set; }

        /// <summary>
        /// Handler called when processing of event has catchup with present.
        /// </summary>
        public ILiveProcessingReached LiveProcessingReachedHandler { get; set; }

        /// <summary>
        /// The username used in UserCredentialFactory to gain access to event store.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// The password used in UserCredentialFactory to gain access to event store.
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// The connection string to the event store cluster.
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// If batchSize is not exceeded within this timeout trigger is fired.
        /// </summary>
        public int TimeOutInMilliSeconds { get; set; }
        /// <summary>
        /// Max batch size before a trigger is fired for event store subscription.
        /// </summary>
        public int BatchSize { get; set; }

        private IEventStoreSubscription _eventStoreSubscription;
        
        /// <summary>
        /// Method called when jobhost starts.
        /// </summary>
        /// <param name="context"></param>
        public void Initialize(ExtensionConfigContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            if (EventStoreConnectionFactory == null)
                EventStoreConnectionFactory = new EventStoreConnectionFactory();

            if (UserCredentialFactory == null)
                UserCredentialFactory = new UserCredentialFactory();
            
            var triggerBindingProvider = new EventTriggerAttributeBindingProvider<EventTriggerAttribute>(
                BuildListener, context.Config, context.Trace);

            if(TimeOutInMilliSeconds ==  0)
                TimeOutInMilliSeconds = 50;
            if(BatchSize == 0)
                BatchSize = 10;

            _eventStoreSubscription = new EventStoreCatchUpSubscriptionObservable(EventStoreConnectionFactory.Create(ConnectionString), 
                LastPosition,
                UserCredentialFactory.CreateAdminCredentials(Username, Password), 
                context.Trace);

            // Register our extension binding providers
            context.Config.RegisterBindingExtensions(
                triggerBindingProvider);
        }
        
        private Task<IListener> BuildListener<TAttribute>(JobHostConfiguration config, 
            TAttribute attribute,
            ITriggeredFunctionExecutor executor, TraceWriter trace)
        {
            IListener listener;
            if (EventStoreListenerFactory == null)
            {
                listener = new EventStoreListener(executor, _eventStoreSubscription, LiveProcessingReachedHandler, trace)
                {
                    BatchSize = BatchSize,
                    TimeOutInMilliSeconds = TimeOutInMilliSeconds
                };
            }
            else
            {
                listener = EventStoreListenerFactory.Create();
            }
            return Task.FromResult<IListener>(listener);
        }
    }
}