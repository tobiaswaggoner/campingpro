using System;
using System.Collections.Immutable;
using System.Linq;
using Newtonsoft.Json;
using ntlt.campingpro.eventstore;
using ntlt.campingpro.state.CustomerSystem;
using ntlt.campingpro.state.Extensions;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace ntlt.campingpro.state.tests.CustomerSystem
{
    [TestFixture(TestName = "Given a CustomerState")]
    public class CustomerStateTests
    {
        [Test]
        public void Assert_that_customers_are_added_and_modified()
        {
            var store = ClientEventStore.Empty;
            var sut = new CustomerState(store);

            sut.Execute(new AddCustomerCommand(Guid.Empty, "TestCustomer"));
            sut.Execute(new ModifyCustomerCommand(Guid.Empty, Option<string>.Some("NewValue")));

            Assert.AreEqual(1, sut.Customers.Count);
            Assert.AreEqual(Guid.Empty, sut.Customers[0].Id);
            Assert.AreEqual("NewValue", sut.Customers[0].Name);
        }

        [Test]
        public void Assert_that_customers_are_added_and_deleted()
        {
            var store = ClientEventStore.Empty;
            var sut = new CustomerState(store);

            sut.Execute(new AddCustomerCommand(Guid.Empty, "TestCustomer"));
            sut.Execute(new DeleteCustomerCommand(Guid.Empty));

            Assert.AreEqual(0, sut.Customers.Count);
        }
        
        [Test]
        public void Assert_events_are_synced()
        {
            var clientEventStore1 = ClientEventStore.Empty;
            var clientState1 = new CustomerState(clientEventStore1);
            var clientEventStore2 = ClientEventStore.Empty;
            var clientState2 = new CustomerState(clientEventStore2);
            var serverEventStore = ServerEventStore.Empty;
            var serverState = new CustomerState(serverEventStore);

            clientState1.Execute(new AddCustomerCommand(Guid.Empty, "TestCustomer"));
            clientState1.Execute(new ModifyCustomerCommand(Guid.Empty, Option<string>.Some("NewValue")));

            clientState2.Execute(new AddCustomerCommand(Guid.NewGuid(), "TestCustomer Number 2"));

            serverState.Execute(new AddCustomerCommand(Guid.NewGuid(), "TestCustomer Number 3"));

            // Sync
            var json1 = JsonConvert.SerializeObject(clientEventStore1.UnsyncedEvents, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All });
            var changes1 = JsonConvert.DeserializeObject<ImmutableList<DomainEvent>>(json1, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All });
            changes1.ForEach(serverEventStore.StoreEvent);

            var json2 = JsonConvert.SerializeObject(clientEventStore2.UnsyncedEvents, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All });
            var changes2 = JsonConvert.DeserializeObject<ImmutableList<DomainEvent>>(json2, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All });
            changes2.ForEach(serverEventStore.StoreEvent);

            clientEventStore1.ApplyChangesFromServer(
                serverEventStore.GetChangesSince(clientEventStore1.Events.LastOrDefault()?.EventId));
            clientEventStore2.ApplyChangesFromServer(
                serverEventStore.GetChangesSince(clientEventStore2.Events.LastOrDefault()?.EventId));

            Assert.IsTrue(clientState1.Customers.Count == clientState2.Customers.Count &&
                          clientState1.Customers.Count == serverState.Customers.Count);

            Assert.IsTrue(Enumerable.Range(0, clientState1.Customers.Count).All(i =>
                clientState1.Customers[i] == clientState2.Customers[i] &&
                clientState1.Customers[i] == serverState.Customers[i]));
        }
    }
}