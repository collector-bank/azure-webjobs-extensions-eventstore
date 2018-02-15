﻿using System;
using System.Text;

namespace Webjobs.Extensions.Eventstore.Sample
{
    public class EventPublisher : IEventPublisher<ResolvedEvent>
    {
        public void Publish(ResolvedEvent item)
        {
            var json = Encoding.UTF8.GetString(item.OriginalEvent.Data);
            JsonConvert.DeserializeObject<Event>(json);
            Console.WriteLine($"Deserialized message: {json}");
        }
    }
}