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
