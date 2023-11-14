using System.Collections.Concurrent;
using System.Text.Json;
using Hangfire.Server;
using Hangfire.States;
using Hangfire.Storage;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace Hangfire.Analytics.ApplicationInsights;

public sealed class HangfireApplicationInsightsFilter : IServerFilter, IApplyStateFilter
{
    private static readonly ConcurrentDictionary<string, IOperationHolder<RequestTelemetry>> Operations = new();

    private readonly TelemetryClient _telemetryClient;

	public HangfireApplicationInsightsFilter(TelemetryClient telemetryClient) => _telemetryClient = telemetryClient;

	public void OnPerforming(PerformingContext context)
	{
		var operationId = context.BackgroundJob.Id;
		var operation = _telemetryClient.StartOperation<RequestTelemetry>(GetJobName(context.BackgroundJob), operationId);
		operation.Telemetry.Properties.Add("JobId", context.BackgroundJob.Id);
		operation.Telemetry.Properties.Add("JobName", GetJobName(context.BackgroundJob));
		operation.Telemetry.Properties.Add("JobArguments", GetJobArguments(context.BackgroundJob));

		Operations.TryAdd(context.BackgroundJob.Id, operation);

		TrackEvent("Job Started", operation);
	}

	public void OnPerformed(PerformedContext context)
	{
		if (Operations.TryRemove(context.BackgroundJob.Id, out var operation))
		{
			var exception = context.Exception is JobPerformanceException performanceException ? performanceException.InnerException : context.Exception;
			TrackEvent(context.Exception != null ? "Job Attempt Failed" : "Job Succeeded", operation, exception);
			_telemetryClient.StopOperation(operation);
			operation.Dispose();
		}
	}

	public void OnStateApplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
	{
		if (context.NewState is FailedState && Operations.TryGetValue(context.BackgroundJob.Id, out var operation))
		{
			TrackEvent("Job Failed", operation);
			_telemetryClient.StopOperation(operation);
			operation.Dispose();
			Operations.TryRemove(context.BackgroundJob.Id, out _);
		}
	}

	public void OnStateUnapplied(ApplyStateContext context, IWriteOnlyTransaction transaction) { }

    private static string GetJobName(BackgroundJob backgroundJob) => $"{backgroundJob.Job.Type.Name}.{backgroundJob.Job.Method.Name}";

    private static string GetJobArguments(BackgroundJob backgroundJob) => JsonSerializer.Serialize(backgroundJob.Job.Args);

    private void TrackEvent(string eventName, IOperationHolder<RequestTelemetry> operation, Exception? exception = null)
	{
		var operationId = operation.Telemetry.Context.Operation.Id;
		var eventTelemetry = new EventTelemetry(eventName)
		{
			Context = { Operation = { Id = operationId, ParentId = operationId } },
			Properties =
			{
				{ "JobId", operation.Telemetry.Properties["JobId"] },
				{ "JobName", operation.Telemetry.Properties["JobName"] },
				{ "JobArguments", operation.Telemetry.Properties["JobArguments"] }
			}
		};

		if (exception != null)
		{
			eventTelemetry.Properties.Add("ErrorMessage", exception.Message);
			eventTelemetry.Properties.Add("StackTrace", exception.StackTrace);

			var exceptionTelemetry = CreateExceptionTelemetry(operation, exception, operationId);
			_telemetryClient.TrackException(exceptionTelemetry);

			operation.Telemetry.Success = false;
			operation.Telemetry.ResponseCode = "Failed";
		}
		else
		{
			operation.Telemetry.Success = true;
			operation.Telemetry.ResponseCode = "Success";
		}

		_telemetryClient.TrackEvent(eventTelemetry);
	}

    private static ExceptionTelemetry CreateExceptionTelemetry(IOperationHolder<RequestTelemetry> operation, Exception exception, string operationId)
	{
		var exceptionTelemetry = new ExceptionTelemetry(exception)
		{
			Context = { Operation = { Id = operationId, ParentId = operationId } },
			SeverityLevel = SeverityLevel.Error,
			Properties =
			{
				{ "JobId", operation.Telemetry.Properties["JobId"] },
				{ "JobName", operation.Telemetry.Properties["JobName"] },
				{ "JobArguments", operation.Telemetry.Properties["JobArguments"] },
				{ "ErrorMessage", exception.Message },
				{ "StackTrace", exception.StackTrace }
			}
		};
		return exceptionTelemetry;
	}
}
