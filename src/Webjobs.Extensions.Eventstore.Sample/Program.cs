using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using Microsoft.Azure;
using Microsoft.Azure.WebJobs;
using SimpleInjector;
using SimpleInjector.Extensions.LifetimeScoping;

namespace Webjobs.Extensions.Eventstore.Sample
{
    class Program
    {
        static void Main()
        {
            var config = new JobHostConfiguration();

            if (config.IsDevelopment)
            {
                config.UseDevelopmentSettings();
            }

            var container = new Container();
            container.Options.DefaultScopedLifestyle = new LifetimeScopeLifestyle();
            InitíalizeContainer(container);

            using (container.BeginLifetimeScope())
            {
                config.UseEventStore(new EventStoreConfig
                {
                    ConnectionString = ConfigurationManager.AppSettings["EventStoreConnectionString"],
                    Username = ConfigurationManager.AppSettings["EventStoreAdminUser"],
                    Password = ConfigurationManager.AppSettings["EventStoreAdminPassword"],
                    LiveProcessingReachedHandler = container.GetInstance<ILiveProcessingReached>(),
                    BatchSize = 100,
                    TimeOutInMilliSeconds = 500,
                    LastPosition = new Position(0,0)
                });
            }

            var jobActivator = new SimpleInjectorJobActivator(container);
            config.JobActivator = jobActivator;
            var host = new JobHost(config);
            host.RunAndBlock();
        }

        private static void InitíalizeContainer(Container container)
        {
            container.Register<ILiveProcessingReached, Handler>();
            container.Register<IEventPublisher<ResolvedEvent>, EventPublisher>();
        }
    }
}
