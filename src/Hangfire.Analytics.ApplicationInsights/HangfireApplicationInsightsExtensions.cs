using Hangfire;
using Hangfire.Analytics.ApplicationInsights;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.Configuration;

public static class HangfireApplicationInsightsExtensions
{
    public static IServiceCollection AddApplicationInsightsTelemetryForHangfire(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<HangfireApplicationInsightsFilter>();

        return services;
    }

    public static IGlobalConfiguration UseApplicationInsightsTelemetry(this IGlobalConfiguration configuration, IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(serviceProvider);

        return configuration.UseFilter(serviceProvider.GetRequiredService<HangfireApplicationInsightsFilter>());
    }

    public static IApplicationBuilder UseApplicationInsightsTelemetryForHangfire(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        GlobalConfiguration.Configuration.UseFilter(app.ApplicationServices.GetRequiredService<HangfireApplicationInsightsFilter>());

        return app;
    }
}
