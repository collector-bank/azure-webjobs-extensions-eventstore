using System;
using EventStore.ClientAPI;

namespace Webjobs.Extensions.Eventstore
{
    public interface IEventStoreConnectionFactory
    {
        Lazy<IEventStoreConnection> Create(string connectionString);
    }
}