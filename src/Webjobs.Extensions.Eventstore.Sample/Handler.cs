using System;
using System.Diagnostics;

namespace Webjobs.Extensions.Eventstore.Sample
{
    public class Handler : ILiveProcessingReached
    {
        public void Handle()
        {
            Console.WriteLine("Catchup handler called, we are now processing live messages.");
        }
    }
}