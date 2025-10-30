using DataBento.Net.Subscriptions;
using DataBento.Net.Tcp;
using Microsoft.Extensions.DependencyInjection;

namespace DataBento.Net;

public static class DataBentoExtensions
{
    public static void AddDataBentoClient(this IServiceCollection services, Action<DataBentoConfig>? configureFunc = null,
        Func<IServiceProvider,IRetryPolicy>? retryPolicyFunc = null) => services.AddDataBentoClient<SubscriptionContainer>(configureFunc, retryPolicyFunc);
    
    public static void AddDataBentoClient<TSubs>(this IServiceCollection services, Action<DataBentoConfig>? configureFunc = null,
        Func<IServiceProvider,IRetryPolicy>? retryPolicyFunc = null)
    where TSubs : class, ISubscriptionContainer
    {
        services.AddSingleton<DataBentoClient>();
        services.AddHostedService<DataBentoClient>(x => x.GetRequiredService<DataBentoClient>());
        if (retryPolicyFunc != null)
        {
            services.AddTransient(retryPolicyFunc);
        }
        else
        {
            services.AddTransient<IRetryPolicy, ExponentialBackoffRetryPolicy>(_ =>
                new ExponentialBackoffRetryPolicy(TimeSpan.FromSeconds(10)));
        }
        services.AddSingleton<TSubs>();
        services.AddSingleton<ISubscriptionContainer, TSubs>(x => x.GetRequiredService<TSubs>());
        if(configureFunc != null)
            services.Configure(configureFunc);
    }
}