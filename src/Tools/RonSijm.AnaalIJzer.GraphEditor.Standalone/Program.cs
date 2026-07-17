using System.IO;
using System.Threading;
using System.Windows;
using Microsoft.Extensions.Logging;
using RonSijm.AnaalIJzer.GraphEditor.Standalone.FileExtension;
using RonSijm.AnaalIJzer.GraphEditor.Standalone.Logging;

namespace RonSijm.AnaalIJzer.GraphEditor.Standalone;

internal static class Program
{
	[STAThread]
	private static void Main(string[] args)
	{
		var logPath = CreateLogPath();
		using var loggerFactory = LoggerFactory.Create(builder =>
		{
			builder.SetMinimumLevel(LogLevel.Trace);
			builder.AddProvider(new FileLoggerProvider(logPath));
		});
		var logger = loggerFactory.CreateLogger("Program");
		logger.LogInformation("Starting AnaalIJzer Graph Editor. Log path: {LogPath}", logPath);
		logger.LogInformation("Arguments: {Arguments}", string.Join(" ", args));
		if (GraphImageExportCommand.TryCreate(args, out var exportCommand, out var exportError))
		{
			RunImageExportCommand(exportCommand, exportError, logger);

			return;
		}

		if (args.Any(argument => string.Equals(argument, "--associate-anl", StringComparison.OrdinalIgnoreCase)))
		{
			var changed = AnaalIJzerFileAssociation.AssociateAnlFiles(logger);
			MessageBox.Show(
				changed ? ".anl files are now associated with the AnaalIJzer Graph Editor." : ".anl files were already associated with the AnaalIJzer Graph Editor.",
				"AnaalIJzer Graph Editor",
				MessageBoxButton.OK,
				MessageBoxImage.Information);
			return;
		}

		var app = new Application();
		RegisterExceptionLogging(app, logger);
		try
		{
			app.Run(new MainWindow(args.FirstOrDefault(), loggerFactory, logPath));
			logger.LogInformation("AnaalIJzer Graph Editor exited normally.");
		}
		catch (Exception exception)
		{
			logger.LogCritical(exception, "AnaalIJzer Graph Editor terminated unexpectedly.");
			MessageBox.Show(
				"Unexpected crash. Details were written to:" + Environment.NewLine + logPath + Environment.NewLine + Environment.NewLine + exception.Message,
				"AnaalIJzer Graph Editor",
				MessageBoxButton.OK,
				MessageBoxImage.Error);
			throw;
		}
	}

	private static void RunImageExportCommand(GraphImageExportCommand? exportCommand, string? exportError, ILogger logger)
	{
		if (exportError is not null)
		{
			Console.Error.WriteLine(exportError);
			Environment.ExitCode = 2;

			return;
		}

		if (exportCommand is null)
		{
			Console.Error.WriteLine("No graph image export command was created.");
			Environment.ExitCode = 2;

			return;
		}

		var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
		RegisterExceptionLogging(app, logger);
		var previousSynchronizationContext = SynchronizationContext.Current;
		SynchronizationContext.SetSynchronizationContext(null);
		try
		{
			Environment.ExitCode = exportCommand.Execute(logger);
		}
		catch (Exception exception)
		{
			logger.LogCritical(exception, "AnaalIJzer Graph Editor image export failed.");
			Console.Error.WriteLine(exception);
			Environment.ExitCode = 1;
		}
		finally
		{
			SynchronizationContext.SetSynchronizationContext(previousSynchronizationContext);
			app.Shutdown();
		}
	}

	private static void RegisterExceptionLogging(Application app, ILogger logger)
	{
		app.DispatcherUnhandledException += (_, args) =>
		{
			logger.LogCritical(args.Exception, "Unhandled WPF dispatcher exception.");
		};
		AppDomain.CurrentDomain.UnhandledException += (_, args) =>
		{
			if (args.ExceptionObject is Exception exception)
			{
				logger.LogCritical(exception, "Unhandled AppDomain exception. Terminating: {IsTerminating}", args.IsTerminating);
				return;
			}

			logger.LogCritical("Unhandled AppDomain exception object: {ExceptionObject}. Terminating: {IsTerminating}", args.ExceptionObject, args.IsTerminating);
		};
		TaskScheduler.UnobservedTaskException += (_, args) =>
		{
			logger.LogError(args.Exception, "Unobserved task exception.");
		};
	}

	private static string CreateLogPath()
	{
		var directory = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
			"AnaalIJzer",
			"GraphEditor",
			"Logs");
		var result = Path.Combine(directory, "GraphEditor-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".log");

		return result;
	}
}
