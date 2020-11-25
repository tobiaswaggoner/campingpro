using System;

namespace ntlt.campingpro.eventstore.tests.TestFixtures
{
    public record TestEvent(Guid Id, string TestProperty) : DomainEvent(Id);

    public static class TestEvents
    {
        public static readonly TestEvent ServerTestEvent1 =
            new(new Guid("00000000-0000-0000-0000-000000000001"), "ServerEvent1");

        public static readonly TestEvent ServerTestEvent2 =
            new(new Guid("00000000-0000-0000-0000-000000000002"), "ServerEvent2");

        public static readonly TestEvent ClientTestEvent1 =
            new(new Guid("00000000-0000-0000-0000-000000000003"), "ClientEvent1");

        public static readonly TestEvent ClientTestEvent2 =
            new(new Guid("00000000-0000-0000-0000-000000000004"), "ClientEvent2");
    }
}