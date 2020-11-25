using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Linq;

namespace ntlt.campingpro.eventstore
{
    public sealed class ServerEventStore
    {
        private ServerEventStore()
        {
        }

        public IObservable<DomainEvent> DomainEvents
        {
            get
            {
                return Observable
                    .FromEventPattern<DomainEvent>(
                        h => OnEventOccured += h,
                        h => OnEventOccured -= h)
                    .Select(x => x.EventArgs);
            }
        }

        public ImmutableList<DomainEvent> Events { get; private set; }

        public static ServerEventStore Empty =>
            new()
            {
                Events = ImmutableList<DomainEvent>.Empty,
            };

        public event EventHandler<DomainEvent> OnEventOccured;

        public void StoreEvent(DomainEvent newEvent)
        {
            Events = Events.Add(newEvent);
            RaiseEvent(newEvent);
        }

        public ImmutableList<DomainEvent> GetChangesSince(Guid? eventId)
        {
            return eventId == null
                ? Events
                : Events
                    .SkipWhile(evt => evt.EventId != eventId)
                    .Skip(1)
                    .ToImmutableList();
        }

        private void RaiseEvent(DomainEvent evt)
        {
            OnEventOccured?.Invoke(this, evt);
        }
    }
}