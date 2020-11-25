using System;
using System.Collections.Immutable;
using System.Linq;
using ntlt.campingpro.eventstore.tests.TestFixtures;
using NUnit.Framework;

namespace ntlt.campingpro.eventstore.tests
{
    [TestFixture(TestName = "Given a ClientEventStore")]
    public class ClientEventStoreTests
    {
        [Test]
        public void Assert_that_events_are_stored()
        {
            var sut = ClientEventStore.Empty;
            var changeEvents = ImmutableList<DomainEvent>.Empty;
            sut.OnEventOccured += (_, evt) => changeEvents = changeEvents.Add(evt);
            var changeEventsFromObservable = ImmutableList<DomainEvent>.Empty;
            sut.DomainEvents.Subscribe(
                evt => changeEventsFromObservable = changeEventsFromObservable.Add(evt)
            );
            sut.StoreEvent(TestEvents.ClientTestEvent1);
            Assert.AreEqual(1, changeEvents.Count);
            Assert.IsTrue(changeEvents.All(ce => ce.GetType() != typeof(RebuildStateEvent)));
            Assert.AreEqual(1, changeEventsFromObservable.Count);
            Assert.AreEqual(0, sut.Events.Count);
            Assert.AreEqual(1, sut.UnsyncedEvents.Count);
            Assert.AreEqual(TestEvents.ClientTestEvent1, sut.UnsyncedEvents[0]);
        }

        [Test]
        public void Assert_that_events_can_be_synced_without_collisions()
        {
            var sut = ClientEventStore.Empty;
            var changeEvents = ImmutableList<DomainEvent>.Empty;
            sut.OnEventOccured += (_, evt) => changeEvents = changeEvents.Add(evt);
            var server = ServerEventStore.Empty;
            sut.StoreEvent(TestEvents.ClientTestEvent1);

            // sync to server
            sut.UnsyncedEvents.ForEach(server.StoreEvent);
            server.StoreEvent(TestEvents.ServerTestEvent1);
            // sync back from server
            var changes = server.GetChangesSince(null);
            sut.ApplyChangesFromServer(changes);

            Assert.AreEqual(2, changeEvents.Count);
            Assert.IsTrue(changeEvents.All(ce => ce.GetType() != typeof(RebuildStateEvent)));
            Assert.AreEqual(2, server.Events.Count);
            Assert.AreEqual(0, sut.UnsyncedEvents.Count);
            AssertThatStoresAreSynced(sut, server);
        }

        [Test]
        public void Assert_that_events_can_be_synced_with_collisions()
        {
            var sut = ClientEventStore.Empty;
            var changeEvents = ImmutableList<DomainEvent>.Empty;
            sut.OnEventOccured += (_, evt) => changeEvents = changeEvents.Add(evt);
            var server = ServerEventStore.Empty;
            sut.StoreEvent(TestEvents.ClientTestEvent1);
            server.StoreEvent(TestEvents.ServerTestEvent1);

            // sync to server
            sut.UnsyncedEvents.ForEach(server.StoreEvent);
            // a second change before the resync happens
            sut.StoreEvent(TestEvents.ClientTestEvent2);

            // sync back from server
            var changes = server.GetChangesSince(null);
            sut.ApplyChangesFromServer(changes);

            Assert.AreEqual(6, changeEvents.Count);
            Assert.IsTrue(changeEvents.Any(ce => ce.GetType() == typeof(RebuildStateEvent)));
            Assert.AreEqual(2, server.Events.Count);
            Assert.AreEqual(1, sut.UnsyncedEvents.Count);
            AssertThatStoresAreSynced(sut, server);
        }


        private static void AssertThatStoresAreSynced(ClientEventStore sut, ServerEventStore server)
        {
            Assert.IsTrue(server.Events.Count == sut.Events.Count);
            Assert.IsTrue(Enumerable.Range(0, server.Events.Count - 1).All(i => server.Events[i] == sut.Events[i]));
        }
    }
}