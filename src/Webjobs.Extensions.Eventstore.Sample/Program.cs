using System.Configuration;
using EventStore.ClientAPI;
using Microsoft.Azure.WebJobs;
using SimpleInjector;
using SimpleInjector.Lifestyles;

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
            container.Options.DefaultScopedLifestyle = new ThreadScopedLifestyle();
            InitíalizeContainer(container);

            using (ThreadScopedLifestyle.BeginScope(container))
            {
                config.UseEventStore(new EventStoreConfig
                {
                    ConnectionString = ConfigurationManager.AppSettings["EventStoreConnectionString"],
                    Username = ConfigurationManager.AppSettings["EventStoreAdminUser"],
                    Password = ConfigurationManager.AppSettings["EventStoreAdminPassword"],
                    LastPosition = new Position(0,0),
                    MaxLiveQueueSize = 500
                });
            }

            var jobActivator = new SimpleInjectorJobActivator(container);
            config.JobActivator = jobActivator;
            var host = new JobHost(config);
            host.RunAndBlock();
        }

        private static void InitíalizeContainer(Container container)
        {
            container.Register<IEventPublisher<ResolvedEvent>, EventPublisher>();
        }
    }
}
