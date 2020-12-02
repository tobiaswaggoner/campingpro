using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using ntlt.campingpro.eventstore;
using ntlt.campingpro.state.CustomerSystem;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Net.Http;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Newtonsoft.Json;

namespace ntlt.campingpro.Client
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var clientEventStore = ClientEventStore.Empty;
            var state = new CustomerState(clientEventStore);

            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");

            builder.Services.AddScoped(
                sp => new HttpClient {BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)});
            builder.Services.AddSingleton(sp => state);
            var viewState = new ViewState();
            builder.Services.AddSingleton(sp => viewState);

            Console.WriteLine($"{builder.HostEnvironment.BaseAddress}synchronizationhub");
            var synchronizationHub = new HubConnectionBuilder()
                .WithUrl($"{builder.HostEnvironment.BaseAddress}synchronizationhub")
                .WithAutomaticReconnect(new ForeverRetryPolicy())
                .Build();

            synchronizationHub.On<string>("ReceiveChangesFromServer", json =>
            {
                var jsonSettings = new JsonSerializerSettings {TypeNameHandling = TypeNameHandling.All};
                var changes = JsonConvert.DeserializeObject<ImmutableList<DomainEvent>>(json, jsonSettings);
                clientEventStore.ApplyChangesFromServer(changes);
                Console.WriteLine("Received data from server " + json);
            });

            var serverChangesAvailable = new Subject<long>();

            synchronizationHub.On<int>("ChangesAvailable", count => { serverChangesAvailable.OnNext(count); });

            // Push client changes to server
            state.Store.DomainEvents
                .Sample(TimeSpan.FromMilliseconds(500))
                .Select( de => 1L)
                .Merge(Observable.Interval(TimeSpan.FromSeconds(5)))
                .Subscribe(async counter =>
                {
                    try
                    {
                        if (synchronizationHub.State != HubConnectionState.Connected || !clientEventStore.UnsyncedEvents.Any()) return;
                        var jsonSettings = new JsonSerializerSettings {TypeNameHandling = TypeNameHandling.All};
                        var json = JsonConvert.SerializeObject(clientEventStore.UnsyncedEvents, jsonSettings);
                        Console.WriteLine("Writing data to server" + json);
                        await synchronizationHub.InvokeCoreAsync("PushChangesToServer", new[] {json});
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("PushChangesToServer: Unhandled exception " + e.Message);
                    }
                });

            // Get changes from server
            serverChangesAvailable.Merge(Observable.Interval(TimeSpan.FromSeconds(5)))
                .Subscribe(async counter =>
                {
                    try
                    {
                        if (synchronizationHub.State != HubConnectionState.Connected) return;
                        await synchronizationHub.InvokeCoreAsync("GetChangesFromServer",
                            new object[] {clientEventStore.Events.LastOrDefault()?.EventId});
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("GetChangesFromServer: Unhandled exception " + e.Message);
                    }
                });


            synchronizationHub.Closed += async exception =>
            {
                await Task.Run(() =>
                {
                    viewState.Connected = false;
                    Console.WriteLine($"SignalRConnection Closed. {exception?.Message}");
                });
            };

            synchronizationHub.Reconnecting += async exception =>
            {
                await Task.Run(() =>
                {
                    viewState.Connected = false;
                    Console.WriteLine($"SignalRConnection Reconnecting {exception?.Message}");
                });
            };
            synchronizationHub.Reconnected += async s =>
            {
                await Task.Run(() =>
                {
                    viewState.Connected = true;
                    Console.WriteLine($"SignalRConnection Reconnected {s}");
                    serverChangesAvailable.OnNext(0);
                });
            };

            TryToStartSynchronizationHub(synchronizationHub, viewState, serverChangesAvailable);
            await builder.Build().RunAsync();
        }

        private static void TryToStartSynchronizationHub(HubConnection synchronizationHub, ViewState viewState, Subject<long> serverChangesAvailable)
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        await synchronizationHub.StartAsync();
                        Console.WriteLine($"Connected to server.");
                        if (!viewState.Connected)
                            viewState.Connected = true;
                        serverChangesAvailable.OnNext(0);
                        return;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Error connecting to server {e?.Message}. Trying to reconnect.");
                        if (viewState.Connected)
                            viewState.Connected = false;
                        await Task.Delay(TimeSpan.FromSeconds(5));
                    }
                }

            });
        }
    }
}