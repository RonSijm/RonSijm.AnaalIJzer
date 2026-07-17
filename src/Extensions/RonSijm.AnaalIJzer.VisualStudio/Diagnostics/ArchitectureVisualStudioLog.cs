using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace RonSijm.AnaalIJzer.VisualStudio.Diagnostics;

internal static class ArchitectureVisualStudioLog
{
	private static readonly Guid PaneGuid = new("1e6f27a0-7148-4ea5-b8f3-6d6e9b86f00f");
	private static readonly ConcurrentQueue<LogEntry> PendingEntries = new();
	private static AsyncPackage? package;

	internal static void Initialize(AsyncPackage packageInstance)
	{
		package = packageInstance;
		Info("Logger initialized.");
		_ = packageInstance.JoinableTaskFactory.RunAsync(FlushPendingEntriesAsync);
	}

	internal static void Info(string message)
	{
		Write(__ACTIVITYLOG_ENTRYTYPE.ALE_INFORMATION, message);
	}

	internal static void Warning(string message)
	{
		Write(__ACTIVITYLOG_ENTRYTYPE.ALE_WARNING, message);
	}

	internal static void Error(string message)
	{
		Write(__ACTIVITYLOG_ENTRYTYPE.ALE_ERROR, message);
	}

	internal static void Exception(string context, Exception exception)
	{
		Error(context + Environment.NewLine + exception);
	}

	private static void Write(__ACTIVITYLOG_ENTRYTYPE entryType, string message)
	{
		var entry = new LogEntry(entryType, FormatMessage(message));
		Trace.WriteLine("[AnaalIJzer] " + entry.Message);

		if (package is null)
		{
			PendingEntries.Enqueue(entry);
			return;
		}

		_ = package.JoinableTaskFactory.RunAsync(async () => await WriteAsync(entry));
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
		if (package is null)
		{
			PendingEntries.Enqueue(entry);
			return;
		}

		try
		{
			await package.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);
			if (await package.GetServiceAsync(typeof(SVsActivityLog)) is IVsActivityLog activityLog)
			{
				activityLog.LogEntry((uint)entry.EntryType, "AnaalIJzer", entry.Message);
			}

			if (await package.GetServiceAsync(typeof(SVsOutputWindow)) is IVsOutputWindow outputWindow)
			{
				var paneGuid = PaneGuid;
				outputWindow.CreatePane(ref paneGuid, "AnaalIJzer", 1, 1);
				outputWindow.GetPane(ref paneGuid, out var pane);
				pane?.OutputStringThreadSafe(entry.Message + Environment.NewLine);
			}
		}
		catch (Exception exception) when (exception is not OperationCanceledException)
		{
			Trace.WriteLine("[AnaalIJzer] Failed to write Visual Studio log entry: " + exception);
		}
	}

	private static string FormatMessage(string message)
	{
		var result = DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss.fff zzz") + " " + message;

		return result;
	}

	private readonly struct LogEntry(__ACTIVITYLOG_ENTRYTYPE entryType, string message)
    {
        public __ACTIVITYLOG_ENTRYTYPE EntryType { get; } = entryType;

        public string Message { get; } = message;
    }
}
