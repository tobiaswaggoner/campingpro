// *********************************************************
// (c) 2020 - 2020 Netzalist GmbH & Co.KG
// *********************************************************

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using ntlt.campingpro.eventstore;

namespace ntlt.campingpro.Server.Hubs
{
    public class SynchronizationHub : Hub
    {
        private readonly ServerEventStore _serverEventStore;

        public SynchronizationHub(ServerEventStore serverEventStore)
        {
            _serverEventStore = serverEventStore;
        }

        public async void GetChangesFromServer(Guid? lastSyncedEvent)
        {
            var changes = _serverEventStore.GetChangesSince(lastSyncedEvent);
            if (!changes.Any()) return;

            var jsonSettings = new JsonSerializerSettings {TypeNameHandling = TypeNameHandling.All};
            await Clients.Caller.SendCoreAsync("ReceiveChangesFromServer", new[] {JsonConvert.SerializeObject(changes, jsonSettings)} );
        }

        public async void PushChangesToServer(string input)
        {
            var jsonSettings = new JsonSerializerSettings {TypeNameHandling = TypeNameHandling.All};
            var changes = JsonConvert.DeserializeObject<ImmutableList<DomainEvent>>(input, jsonSettings);
            changes?.ForEach( change => _serverEventStore.StoreEvent(change));
            await Clients.All.SendCoreAsync("ChangesAvailable", new object[] {(long) changes!.Count()} );
        }
    }
}