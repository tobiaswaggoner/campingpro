using System;

namespace ntlt.campingpro.eventstore
{
    public abstract record DomainEvent(Guid EventId)
    {
        public string EventName => GetType().FullName;
    }
}