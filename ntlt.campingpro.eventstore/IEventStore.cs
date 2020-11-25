using System;
using System.Collections.Immutable;

namespace ntlt.campingpro.eventstore
{
    public interface IEventStore
    {
        IObservable<DomainEvent> DomainEvents { get; }
        ImmutableList<DomainEvent> Events { get; }
        event EventHandler<DomainEvent> OnEventOccured;
        void StoreEvent(DomainEvent newEvent);
    }
}