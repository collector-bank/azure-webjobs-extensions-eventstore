# Azure Webjobs Eventstore extension

This repo contains binding extension for the Eventstore (https://github.com/EventStore/EventStore).

## Getting Started

When creating the Jobhost use the following extension method to bind the triggers.

```c#
config.UseEventStore(new EventStoreConfig
        {
            ConnectionString = "ConnectTo=tcp://localhost:1113;HeartbeatTimeout=20000",
            Username = "admin",
            Password = "changeit",
            LastPosition = new Position(0,0),
            MaxLiveQueueSize = 500
        });
```

```c#        
[Singleton(Mode = SingletonMode.Listener)]
public void ProcessQueueMessage([EventTrigger(BatchSize = 10, TimeOutInMilliSeconds = 20)] IEnumerable<ResolvedEvent> events)
{
    //Handle the delivered events
}

[Disable(WebJobDisabledSetting)]
public void LiveProcessingStarted([LiveProcessingStarted] LiveProcessingStartedContext context)
{
    //Handle the swap from catchup to live mode
}
```

## Authors

* **Michael Hultman* - *Initial work* -

See also the list of [contributors](https://github.com/your/project/contributors) who participated in this project.

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details

## Acknowledgments

* Hat tip to anyone who's code was used
* Inspiration
* etc
