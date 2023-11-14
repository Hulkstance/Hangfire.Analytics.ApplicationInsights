[![NuGet](https://img.shields.io/nuget/v/Hulkstance.Hangfire.Analytics.ApplicationInsights.svg)](https://www.nuget.org/packages/Hulkstance.Hangfire.Analytics.ApplicationInsights)
[![NuGet](https://img.shields.io/nuget/dt/Hulkstance.Hangfire.Analytics.ApplicationInsights.svg)](https://www.nuget.org/packages/Hulkstance.Hangfire.Analytics.ApplicationInsights)
![GitHub](https://img.shields.io/github/license/Hulkstance/Hangfire.Analytics.ApplicationInsights)

# Hangfire.Analytics.ApplicationInsights

Integrates Hangfire with Application Insights for monitoring and analytics. You can easily build metrics dashboards and create alerts in Azure.

![Example](./example.png)

# How it Works

`Hangfire.Analytics.ApplicationInsights` integrates directly with Hangfire’s job lifecycle by implementing a custom filter that intercepts job execution. This filter tracks job performance metrics, detects exceptions, and records job failure states, logging all this information to Application Insights. This enables developers to monitor job performance, identify failures, and analyze execution patterns using Application Insights’ powerful dashboard.

## Installation

Using NuGet:

```shell
Install-Package Hulkstance.Hangfire.Analytics.ApplicationInsights
```

Or via the .NET CLI:

```shell
dotnet add package Hulkstance.Hangfire.Analytics.ApplicationInsights
```

## Usage

### 1. Register the Hangfire Filter

Add the Hangfire filter to your service collection to begin capturing data from Hangfire job executions:

```csharp
services.AddApplicationInsightsTelemetryForHangfire();
```

### 2. Configure Hangfire to Use This Filter

After registering the filter, ensure Hangfire uses this filter by configuring it within your application startup.

Normally, you would set up Hangfire like this:

```csharp
services.AddHangfire(config =>
    config.UseRedisStorage("<your_connection_string>")
        // ... other configurations
);
```

You can make Hangfire use this filter by adding the following line:

```csharp
services.AddHangfire((serviceProvider, config) => // use this overload to access IServiceProvider
    config.UseRedisStorage("<your_connection_string>")
        // ... other configurations
        .UseApplicationInsightsTelemetry(serviceProvider) // this line
);
```

Alternatively, for a more streamlined approach, particularly when configuring your app's pipeline, use the provided `IApplicationBuilder` extension:

```csharp
app.UseApplicationInsightsTelemetryForHangfire();
```

Add this line to your `Program.cs` or within the `Configure` method of your startup class.

This setup ensures that all your Hangfire jobs are now tracked in Application Insights, allowing you to view job metrics, identify and investigate failures, and gain deeper insights into your background processes.

## Example Application Insights Queries

You can use the following queries as a starting point to analyze and set up alerts for specific conditions related to Hangfire job processing:

### View All Hangfire Job Executions

```kql
customEvents
| where customDimensions.JobId != ""
| project timestamp, name, customDimensions
```

### View Specific Hangfire Job Failures

Create an alert for jobs that fail by matching the event name exactly:

```kql
customEvents
| where timestamp > ago(1d) and name == "Job Attempt Failed"
| project timestamp, name, customDimensions
```

### Monitor Job Failures Over Time

Visualize the frequency of job failures over time to spot trends:

```kql
customEvents
| where name == "Job Attempt Failed"
| summarize count() by bin(timestamp, 1h)
| render timechart
```

### Count of Failed Jobs by Type

Identify which job types are failing most often:

```kql
customEvents
| where name == "Job Attempt Failed"
| summarize count() by tostring(customDimensions.JobName)
| order by count_ desc
```

These are just starting points! You can do way more with Application Insights.

# Contributing

Contributions, issues, and feature requests are welcome! Feel free to check the [issues page](https://github.com/Hulkstance/Hangfire.Analytics.ApplicationInsights/issues).
