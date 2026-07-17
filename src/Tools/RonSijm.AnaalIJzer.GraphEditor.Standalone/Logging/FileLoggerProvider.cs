using System.IO;
using Microsoft.Extensions.Logging;

namespace RonSijm.AnaalIJzer.GraphEditor.Standalone.Logging;

internal sealed class FileLoggerProvider : ILoggerProvider
{
	private readonly object syncRoot = new();
	private readonly StreamWriter writer;

	public FileLoggerProvider(string path)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(path)!);
		writer = new StreamWriter(new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
		{
			AutoFlush = true
		};
	}

	public ILogger CreateLogger(string categoryName)
	{
		var result = new FileLogger(categoryName, Write);

		return result;
	}

	public void Dispose()
	{
		lock (syncRoot)
		{
			writer.Dispose();
		}
	}

	private void Write(string categoryName, LogLevel logLevel, EventId eventId, string message, Exception? exception)
	{
		lock (syncRoot)
		{
			writer.Write(DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss.fff zzz"));
			writer.Write(" [");
			writer.Write(logLevel);
			writer.Write("] ");
			writer.Write(categoryName);
			if (eventId.Id != 0 || !string.IsNullOrWhiteSpace(eventId.Name))
			{
				writer.Write(" ");
				writer.Write(eventId.Id);
				if (!string.IsNullOrWhiteSpace(eventId.Name))
				{
					writer.Write(":");
					writer.Write(eventId.Name);
				}
			}

			writer.Write(" - ");
			writer.WriteLine(message);
			if (exception is not null)
			{
				writer.WriteLine(exception);
			}
		}
	}

	private sealed class FileLogger(string categoryName, Action<string, LogLevel, EventId, string, Exception?> write)
        : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
		{
			return null;
		}

		public bool IsEnabled(LogLevel logLevel)
		{
			var result = logLevel != LogLevel.None;

			return result;
		}

		public void Log<TState>(
			LogLevel logLevel,
			EventId eventId,
			TState state,
			Exception? exception,
			Func<TState, Exception?, string> formatter)
		{
			if (!IsEnabled(logLevel))
			{
				return;
			}

			var message = formatter(state, exception);
			write(categoryName, logLevel, eventId, message, exception);
		}
	}
}
