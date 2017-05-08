using System.Diagnostics;

namespace Webjobs.Extensions.Eventstore.Sample
{
    public class Handler : ILiveProcessingReached
    {
        public void Handle()
        {
            Trace.TraceInformation("Catchup handler called, we are now processing live messages.");
        }
    }
}