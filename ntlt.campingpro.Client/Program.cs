using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ntlt.campingpro.eventstore;
using ntlt.campingpro.state.CustomerSystem;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ntlt.campingpro.Client
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var state = new CustomerState(ClientEventStore.Empty);
            state.Execute(new AddCustomerCommand(Guid.NewGuid(), "Tobias Waggoner"));
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");

            builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
            builder.Services.AddSingleton(sp => state);
            builder.Services.AddSingleton(sp => new ViewState());

            await builder.Build().RunAsync();
        }
    }
}
