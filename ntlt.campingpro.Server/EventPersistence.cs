// *********************************************************
// (c) 2020 - 2020 Netzalist GmbH & Co.KG
// *********************************************************

using System.Collections.Immutable;
using System.IO;
using Newtonsoft.Json;
using ntlt.campingpro.eventstore;

namespace ntlt.campingpro.Server
{
    // Interims class - just for test purposes
    public static class EventPersistence
    {
        public static void StoreEvent(DomainEvent evt)
        {
            using var writer = new StreamWriter("events.json", true);
            writer.WriteLine(JsonConvert.SerializeObject(evt,new JsonSerializerSettings{TypeNameHandling = TypeNameHandling.All, Formatting = Formatting.None}));
        }
        
        public static ImmutableList<DomainEvent> ReloadAll()
        {
            if(!File.Exists("events.json")) return ImmutableList<DomainEvent>.Empty;

            using var reader = new StreamReader("events.json");
            var result = ImmutableList<DomainEvent>.Empty;
            while (!reader.EndOfStream)
            {
                var evt = reader.ReadLine();
                result = result.Add(JsonConvert.DeserializeObject<DomainEvent>(evt,
                    new JsonSerializerSettings
                        {TypeNameHandling = TypeNameHandling.All, Formatting = Formatting.None}));
            }

            return result;
        }

    }
}