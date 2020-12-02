using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Linq;

namespace ntlt.campingpro.eventstore
{
    public sealed record RebuildStateEvent() : DomainEvent(Guid.NewGuid());

    public sealed class ClientEventStore : IEventStore
    {
        private ClientEventStore()
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
        public ImmutableList<DomainEvent> UnsyncedEvents { get; private set; }

        public static ClientEventStore Empty =>
            new()
            {
                Events = ImmutableList<DomainEvent>.Empty,
                UnsyncedEvents = ImmutableList<DomainEvent>.Empty,
            };

        public event EventHandler<DomainEvent> OnEventOccured;

        public void StoreEvent(DomainEvent newEvent)
        {
            UnsyncedEvents = UnsyncedEvents.Add(newEvent);
            RaiseEvent(newEvent);
        }

        public void ApplyChangesFromServer(ImmutableList<DomainEvent> changes)
        {
            // Skip all that have already arrived earlier.
            var newChanges = changes
                .Where(change => Events.All(evt => evt.EventId != change.EventId))
                .ToImmutableList();

            // there are no Unsynced changes or
            // the changes start with our Unsynced Events --> fast forward
            // only raise the new events
            if (UnsyncedEvents.Count == 0 || newChanges.Count >= UnsyncedEvents.Count &&
                Enumerable.Range(0, UnsyncedEvents.Count)
                    .All(i => newChanges[i].EventId == UnsyncedEvents[i].EventId))
            {
                newChanges.Skip(UnsyncedEvents.Count)
                    .ToImmutableList()
                    .ForEach(RaiseEvent);

                UnsyncedEvents = ImmutableList<DomainEvent>.Empty;
                Events = Events.AddRange(newChanges);
                return;
            }

            // We have a potential conflict. Our changes must be re-applied after
            // the server changes. Rebuild State from scratch.

            Events = Events.TakeWhile(evt => evt.EventId != newChanges.First().EventId).ToImmutableList()
                .AddRange(newChanges);
            UnsyncedEvents = UnsyncedEvents
                .Where(ue => newChanges.All(change => change.EventId != ue.EventId))
                .ToImmutableList();

            // Reset the state(s) to empty
            RaiseEvent(new RebuildStateEvent());
            //Raise all synced events
            Events.ForEach(RaiseEvent);
            //Raise unsynced events
            UnsyncedEvents.ForEach(RaiseEvent);
        }

        private void RaiseEvent(DomainEvent evt)
        {
            OnEventOccured?.Invoke(this, evt);
        }
    }
}