using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace DataBento.Net.History;

public static class BentoHistoryClientExtensions
{
    public static void AddBentoHistoryClient(this IServiceCollection services, Func<IServiceProvider, string> getApiKeyFunc)
    {
        services.AddTransient<BentoHistoryClient>();
        services.AddHttpClient(BentoHistoryClient.HttpClientName, (svc, client) =>
        {
            client.BaseAddress = new Uri("https://hist.databento.com/");
            var apiKey = getApiKeyFunc(svc);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(Encoding.UTF8.GetBytes($"{apiKey}:")));
        });
    }
}