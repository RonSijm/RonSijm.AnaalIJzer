using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;

namespace RonSijm.AnaalIJzer.VisualStudio.Diagnostics;

internal static class ArchitectureVisualStudioLog
{
	private const string ActivityLogServiceTypeName = "Microsoft.VisualStudio.Shell.Interop.SVsActivityLog, Microsoft.VisualStudio.Interop";
	private const string OutputWindowServiceTypeName = "Microsoft.VisualStudio.Shell.Interop.SVsOutputWindow, Microsoft.VisualStudio.Interop";
	private const uint InformationEntryType = 0;
	private const uint WarningEntryType = 1;
	private const uint ErrorEntryType = 2;
	private static readonly Guid PaneGuid = new("1e6f27a0-7148-4ea5-b8f3-6d6e9b86f00f");
	private static readonly ConcurrentQueue<LogEntry> PendingEntries = new();
	private static object? _package;

	internal static void Initialize(object packageInstance)
	{
		_package = packageInstance;
		Info("Logger initialized.");
		RunFireAndForget(FlushPendingEntriesAsync);
	}

	internal static void Info(string message)
	{
		Write(InformationEntryType, message);
	}

	internal static void Warning(string message)
	{
		Write(WarningEntryType, message);
	}

	internal static void Error(string message)
	{
		Write(ErrorEntryType, message);
	}

	internal static void Exception(string context, Exception exception)
	{
		Error(context + Environment.NewLine + exception);
	}

	private static void Write(uint entryType, string message)
	{
		var entry = new LogEntry(entryType, FormatMessage(message));
		Trace.WriteLine("[AnaalIJzer] " + entry.Message);

		if (_package is null)
		{
			PendingEntries.Enqueue(entry);
			return;
		}

		RunFireAndForget(() => WriteAsync(entry));
	}

	private static async Task FlushPendingEntriesAsync()
	{
		while (PendingEntries.TryDequeue(out var entry))
		{
			await WriteAsync(entry);
		}
	}

	private static async Task WriteAsync(LogEntry entry)
	{
		if (_package is null)
		{
			PendingEntries.Enqueue(entry);
			return;
		}

		try
		{
			await WriteActivityLogAsync(entry);
			await WriteOutputWindowAsync(entry);
		}
		catch (Exception exception) when (exception is not OperationCanceledException)
		{
			Trace.WriteLine("[AnaalIJzer] Failed to write Visual Studio log entry: " + exception);
		}
	}

	private static async Task WriteActivityLogAsync(LogEntry entry)
	{
		var serviceType = Type.GetType(ActivityLogServiceTypeName, throwOnError: false);
		if (_package is null || serviceType is null)
		{
			return;
		}

		var service = await GetServiceAsync(_package, serviceType);
		if (service is null)
		{
			return;
		}

		var logMethod = service.GetType().GetMethod("LogEntry", BindingFlags.Instance | BindingFlags.Public);
		if (logMethod is null)
		{
			return;
		}

		object?[] arguments = [entry.EntryType, "AnaalIJzer", entry.Message];
		logMethod.Invoke(service, arguments);
	}

	private static async Task WriteOutputWindowAsync(LogEntry entry)
	{
		var serviceType = Type.GetType(OutputWindowServiceTypeName, throwOnError: false);
		if (_package is null || serviceType is null)
		{
			return;
		}

		var service = await GetServiceAsync(_package, serviceType);
		if (service is null)
		{
			return;
		}

		var serviceRuntimeType = service.GetType();
		var createPaneMethod = serviceRuntimeType.GetMethod("CreatePane", BindingFlags.Instance | BindingFlags.Public);
		var getPaneMethod = serviceRuntimeType.GetMethod("GetPane", BindingFlags.Instance | BindingFlags.Public);
		if (createPaneMethod is null || getPaneMethod is null)
		{
			return;
		}

		var paneGuid = PaneGuid;
		object?[] createArguments = [paneGuid, "AnaalIJzer", 1, 1];
		createPaneMethod.Invoke(service, createArguments);
		paneGuid = createArguments[0] is Guid updatedGuid ? updatedGuid : PaneGuid;

		var getPaneArguments = new object?[] { paneGuid, null };
		getPaneMethod.Invoke(service, getPaneArguments);
		var pane = getPaneArguments[1];
		if (pane is null)
		{
			return;
		}

		var outputMethod = pane.GetType().GetMethod("OutputStringThreadSafe", BindingFlags.Instance | BindingFlags.Public);
		if (outputMethod is null)
		{
			return;
		}

		object?[] outputArguments = [entry.Message + Environment.NewLine];
		outputMethod.Invoke(pane, outputArguments);
	}

	private static void RunFireAndForget(Func<Task> action)
	{
		_ = Task.Run(action);
	}

	private static async Task<object?> GetServiceAsync(object packageInstance, Type serviceType)
	{
		var method = packageInstance.GetType().GetMethod("GetServiceAsync", [typeof(Type)]);
		if (method is null)
		{
			return null;
		}

		var invocationResult = method.Invoke(packageInstance, [serviceType]);
		if (invocationResult is not Task task)
		{
			return null;
		}

		await task.ConfigureAwait(false);

		var resultProperty = task.GetType().GetProperty("Result", BindingFlags.Instance | BindingFlags.Public);
		var result = resultProperty?.GetValue(task);

		return result;
	}

	private static string FormatMessage(string message)
	{
		var result = DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss.fff zzz") + " " + message;

		return result;
	}

	private readonly struct LogEntry(uint entryType, string message)
	{
		public uint EntryType { get; } = entryType;

		public string Message { get; } = message;
	}
}
