using System;
using System.Collections.Immutable;
using ntlt.campingpro.eventstore.tests.TestFixtures;
using NUnit.Framework;

namespace ntlt.campingpro.eventstore.tests
{
    [TestFixture(TestName = "Given a ServerEventStore")]
    public class ServerEventStoreTests
    {
        [Test]
        public void Assert_that_events_are_stored()
        {
            var sut = ServerEventStore.Empty;
            var changeEvents = ImmutableList<DomainEvent>.Empty;
            sut.OnEventOccured += (_, evt) => changeEvents = changeEvents.Add(evt);
            var changeEventsFromObservable = ImmutableList<DomainEvent>.Empty;
            sut.DomainEvents.Subscribe(
                evt => changeEventsFromObservable = changeEventsFromObservable.Add(evt)
            );
            sut.StoreEvent(TestEvents.ServerTestEvent1);
            
            Assert.AreEqual(1, changeEvents.Count);
            Assert.AreEqual(1, changeEventsFromObservable.Count);
            Assert.AreEqual(1, sut.Events.Count);
            Assert.AreEqual(TestEvents.ServerTestEvent1, sut.Events[0] );
            Assert.AreEqual(0, sut.GetChangesSince(TestEvents.ServerTestEvent1.EventId).Count);
        }
        
        [Test]
        public void Assert_that_new_events_are_found()
        {
            var sut = ServerEventStore.Empty;
            var changeEvents = ImmutableList<DomainEvent>.Empty;
            sut.OnEventOccured += (_, evt) => changeEvents = changeEvents.Add(evt);
            sut.StoreEvent(TestEvents.ServerTestEvent1);
            sut.StoreEvent(TestEvents.ServerTestEvent2);

            Assert.AreEqual(2, changeEvents.Count);
            Assert.AreEqual(2, sut.Events.Count);
            Assert.AreEqual(TestEvents.ServerTestEvent1, sut.Events[0] );
            Assert.AreEqual(TestEvents.ServerTestEvent2, sut.Events[1]);
            Assert.AreEqual(1, sut.GetChangesSince(TestEvents.ServerTestEvent1.EventId).Count);
            Assert.AreEqual(TestEvents.ServerTestEvent2, sut.GetChangesSince(TestEvents.ServerTestEvent1.EventId)[0]);
        }

    }
}