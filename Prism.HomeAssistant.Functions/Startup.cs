using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Prism.HomeAssistant.Functions;

[assembly: FunctionsStartup(typeof(Startup))]

namespace Prism.HomeAssistant.Functions;

public class Startup : FunctionsStartup
{
    public override void Configure(IFunctionsHostBuilder builder)
    {
        builder.Services.AddHttpClient();
    }
}