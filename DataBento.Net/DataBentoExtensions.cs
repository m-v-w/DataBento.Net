using DataBento.Net.Subscriptions;
using DataBento.Net.Tcp;
using Microsoft.Extensions.DependencyInjection;

namespace DataBento.Net;

public static class DataBentoExtensions
{
    public static void AddDataBentoClient(this IServiceCollection services, Action<DataBentoConfig> configure)
    {
        services.AddSingleton<DataBentoClient>();
        services.AddHostedService<DataBentoClient>(x => x.GetRequiredService<DataBentoClient>());
        services.AddTransient<IRetryPolicy, ExponentialBackoffRetryPolicy>();
        services.AddSingleton<ISubscriptionContainer, SubscriptionContainer>();
        services.Configure(configure);
    }
    public static void AddDataBentoClient<TRetry, TSubs>(this IServiceCollection services)
    where TRetry : class, IRetryPolicy where TSubs : class, ISubscriptionContainer
    {
        services.AddSingleton<DataBentoClient>();
        services.AddHostedService<DataBentoClient>(x => x.GetRequiredService<DataBentoClient>());
        services.AddTransient<IRetryPolicy, TRetry>();
        services.AddSingleton<ISubscriptionContainer, TSubs>();
    }
}