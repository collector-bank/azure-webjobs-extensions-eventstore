using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using Microsoft.Azure.WebJobs.Extensions.Bindings;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Azure.WebJobs.Host.Protocols;
using Microsoft.Azure.WebJobs.Host.Triggers;

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

    internal class LiveProcessingStartedTriggerValue
    {
        public EventStoreCatchUpSubscription Subscription { get; }

        public LiveProcessingStartedTriggerValue(EventStoreCatchUpSubscription subscription)
        {
            Subscription = subscription;
        }
    }

    internal class LiveProcessingStartedAttributeBindingProvider : ITriggerBindingProvider
    {
        private readonly IEventStoreSubscription _eventStoreSubscription;
        private readonly TraceWriter _traceWriter;

        public LiveProcessingStartedAttributeBindingProvider(IEventStoreSubscription eventStoreSubscription, TraceWriter traceWriter)
        {
            _eventStoreSubscription = eventStoreSubscription;
            _traceWriter = traceWriter;
        }

        public Task<ITriggerBinding> TryCreateAsync(TriggerBindingProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            ParameterInfo parameter = context.Parameter;
            var attribute = parameter.GetCustomAttribute<LiveProcessingStartedAttribute>(inherit: false);
            if (attribute == null)
            {
                return Task.FromResult<ITriggerBinding>(null);
            }

            if (parameter.ParameterType != typeof(LiveProcessingStartedContext))
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture,
                    "Can't bind LiveProcessingStartedAttribute to type '{0}'.", parameter.ParameterType));
            }

            return Task.FromResult<ITriggerBinding>(new LiveProcessingStartedTriggerBinding(parameter, _eventStoreSubscription, _traceWriter));
        }

        internal class LiveProcessingStartedTriggerBinding : ITriggerBinding
        {
            private readonly ParameterInfo _parameter;
            private readonly IEventStoreSubscription _eventStoreSubscription;
            private readonly TraceWriter _trace;

            public LiveProcessingStartedTriggerBinding(ParameterInfo parameter, IEventStoreSubscription eventStoreSubscription, TraceWriter trace)
            {
                _parameter = parameter;
                _eventStoreSubscription = eventStoreSubscription;
                _trace = trace;
                BindingDataContract = CreateBindingDataContract();
            }

            public IReadOnlyDictionary<string, Type> BindingDataContract { get; }

            public Type TriggerValueType => typeof(LiveProcessingStartedTriggerValue);

            public Task<ITriggerData> BindAsync(object value, ValueBindingContext context)
            {
                if (value is string)
                {
                    throw new NotSupportedException("LiveProcessingStartedTrigger does not support Dashboard invocation.");
                }

                var triggerValue = value as LiveProcessingStartedTriggerValue;
                IValueBinder valueBinder = new LiveProcessingStartedTriggerValueBinder(_parameter, triggerValue);
                return Task.FromResult<ITriggerData>(new TriggerData(valueBinder, GetBindingData(triggerValue)));
            }

            public Task<IListener> CreateListenerAsync(ListenerFactoryContext context)
            {
                return null;
            }

            public ParameterDescriptor ToParameterDescriptor()
            {
                return new LiveProcessingStartedTriggerParameterDescriptor
                {
                    Name = _parameter.Name,
                    DisplayHints = new ParameterDisplayHints
                    {
                        Prompt = "Live processing trigger",
                        Description = "Live processing trigger fired",
                        DefaultValue = "---"
                    }
                };
            }

            private IReadOnlyDictionary<string, object> GetBindingData(LiveProcessingStartedTriggerValue value)
            {
                Dictionary<string, object> bindingData = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                bindingData.Add("LiveProcessingStartedContext", value);
                
                return bindingData;
            }

            private IReadOnlyDictionary<string, Type> CreateBindingDataContract()
            {
                Dictionary<string, Type> contract = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
                contract.Add("LiveProcessingStarted", typeof(LiveProcessingStartedTriggerValueBinder));

                return contract;
            }

            private class LiveProcessingStartedTriggerParameterDescriptor : TriggerParameterDescriptor
            {
                public override string GetTriggerReason(IDictionary<string, string> arguments)
                {
                    return string.Format("Live processing started trigger fired at {0}", DateTime.Now.ToString("o"));
                }
            }

            private class LiveProcessingStartedTriggerValueBinder : ValueBinder
            {
                private readonly object _value;

                public LiveProcessingStartedTriggerValueBinder(ParameterInfo parameter, LiveProcessingStartedTriggerValue value)
                    : base(parameter.ParameterType)
                {
                    _value = value;
                }

                public override Task<object> GetValueAsync()
                {
                    if (Type == typeof(LiveProcessingStartedContext))
                    {
                        var triggerData = (LiveProcessingStartedTriggerValue)_value;
                        var data = new LiveProcessingStartedContext(triggerData.Subscription);
                        return Task.FromResult<object>(data);
                    }
                    return Task.FromResult(_value);
                }

                public override string ToInvokeString()
                {
                    return "Live processing trigger";
                }
            }
        }
    }
}