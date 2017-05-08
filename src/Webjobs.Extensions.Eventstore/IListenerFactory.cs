using Microsoft.Azure.WebJobs.Host.Listeners;

namespace Webjobs.Extensions.Eventstore
{
    public interface IListenerFactory
    {
        IListener Create();
    }
}