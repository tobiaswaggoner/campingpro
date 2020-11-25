using System;
using ntlt.campingpro.eventstore.tests.TestFixtures;
using NUnit.Framework;

namespace ntlt.campingpro.eventstore.tests
{
    [TestFixture(TestName = "Given a DomainEvent")]
    public class DomainEventTests
    {
        [Test]
        public void Assert_that_the_name_is_correct()
        {
            var sut = TestEvents.ServerTestEvent1;
            Assert.AreEqual(sut.GetType().FullName, sut.EventName);
            Assert.AreEqual(new Guid("00000000-0000-0000-0000-000000000001"), sut.EventId);
        }
    }
}