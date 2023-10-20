using System.Text.Json;
using Hangfire.Server;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace Hangfire.Analytics.ApplicationInsights;

public sealed class HangfireApplicationInsightsFilter : IServerFilter
{
    private readonly TelemetryClient _telemetryClient;

	public HangfireApplicationInsightsFilter(TelemetryClient telemetryClient) => _telemetryClient = telemetryClient;

	public void OnPerforming(PerformingContext filterContext)
	{
		var operation = _telemetryClient.StartOperation<RequestTelemetry>(GetJobName(filterContext.BackgroundJob));
		operation.Telemetry.Properties.Add("JobId", filterContext.BackgroundJob.Id);
		operation.Telemetry.Properties.Add("Arguments", GetJobArguments(filterContext.BackgroundJob));

		filterContext.Items["ApplicationInsightsOperation"] = operation;
	}

	public void OnPerformed(PerformedContext filterContext)
	{
		if (filterContext.Items["ApplicationInsightsOperation"] is not IOperationHolder<RequestTelemetry> operation) return;

		if (filterContext.Exception == null || filterContext.ExceptionHandled)
		{
			operation.Telemetry.Success = true;
			operation.Telemetry.ResponseCode = "Success";
		}
		else
		{
			operation.Telemetry.Success = false;
			operation.Telemetry.ResponseCode = "Failed";

			var operationId = operation.Telemetry.Context.Operation.Id;

			var exceptionTelemetry = new ExceptionTelemetry(filterContext.Exception);
			exceptionTelemetry.Context.Operation.Id = operationId;
			exceptionTelemetry.Context.Operation.ParentId = operationId;

			_telemetryClient.TrackException(exceptionTelemetry);
		}

		_telemetryClient.StopOperation(operation);
	}

	private static string GetJobName(BackgroundJob backgroundJob) => $"{backgroundJob.Job.Type.Name}.{backgroundJob.Job.Method.Name}";

	private static string GetJobArguments(BackgroundJob backgroundJob) => JsonSerializer.Serialize(backgroundJob.Job.Args);
}
