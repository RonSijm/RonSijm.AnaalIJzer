using System.Collections.Immutable;
using System.Text;
using Microsoft.AspNetCore.Components.Web;
using RonSijm.AnaalIJzer.Application;
using RonSijm.AnaalIJzer.Core.Findings;
using Spectre.Console;

namespace RonSijm.AnaalIJzer.Arse.Components;

public partial class App
{
	private async Task OnInspectionReportKeyDown(KeyboardEventArgs args, Func<KeyboardEventArgs, Task> scroll)
	{
		if (IsEscape(args))
		{
			await ReturnToInspectionForm();

			return;
		}

		await scroll(args);
	}

	private Task OnInspectionResultKeyUp(KeyboardEventArgs args)
	{
		var result = IsEscape(args) ? ReturnToInspectionForm() : Task.CompletedTask;

		return result;
	}

	private Task OnInspectionSaveKey(KeyboardEventArgs args)
	{
		var result = IsEscape(args) ? CancelInspectionSave() : Task.CompletedTask;

		return result;
	}

	private Task ShowInspectionSave()
	{
		_selectingInspectionOutput = true;
		ClearStatus();

		return Task.CompletedTask;
	}

	private Task CancelInspectionSave()
	{
		_selectingInspectionOutput = false;
		ClearStatus();

		return Task.CompletedTask;
	}

	private Task ReturnToInspectionForm()
	{
		ClearInspectionResult();
		ClearStatus();

		return Task.CompletedTask;
	}

	private async Task SaveInspectionAsync()
	{
		if (_inspectionReport is null)
		{
			return;
		}

		try
		{
			var outputPath = NullIfWhiteSpace(_outputPath) is { } path
				? Path.GetFullPath(path)
				: throw new ApplicationOperationException("Select an output file.");
			Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
			await File.WriteAllTextAsync(outputPath, _inspectionReport, new UTF8Encoding(false));
			_outputPath = outputPath;
			_selectingInspectionOutput = false;
			SetStatus("Saved", $"Wrote {outputPath}", Color.Green);
		}
		catch (ApplicationOperationException ex)
		{
			SetStatus("Cannot save", ex.Message, Color.Yellow);
		}
		catch (Exception ex)
		{
			SetStatus("Cannot save", ex.Message, Color.Red);
		}
	}

	private bool HasInspectionExceptionFindings
	{
		get
		{
			var result = _inspectionFindings.Any(finding => string.Equals(finding.Category, "ARCH017", StringComparison.Ordinal));

			return result;
		}
	}

	private ImmutableArray<ArchitectureFinding> FilteredInspectionExceptionFindings
	{
		get
		{
			var result = _inspectionFindings
				.Where(finding => string.Equals(finding.Category, "ARCH017", StringComparison.Ordinal))
				.Where(MatchesInspectionExceptionFilter)
				.ToImmutableArray();

			return result;
		}
	}

	private Task ToggleInspectionExceptionFilter(string status)
	{
		switch (status)
		{
			case "Invalid":
				_showInvalidInspectionExceptions = !_showInvalidInspectionExceptions;
				break;
			case "ExpiringSoon":
				_showExpiringSoonInspectionExceptions = !_showExpiringSoonInspectionExceptions;
				break;
			case "Expired":
				_showExpiredInspectionExceptions = !_showExpiredInspectionExceptions;
				break;
			case "Stale":
				_showStaleInspectionExceptions = !_showStaleInspectionExceptions;
				break;
		}

		return Task.CompletedTask;
	}

	private bool MatchesInspectionExceptionFilter(ArchitectureFinding finding)
	{
		var result = finding.State switch
		{
			"Invalid" => _showInvalidInspectionExceptions,
			"ExpiringSoon" => _showExpiringSoonInspectionExceptions,
			"Expired" => _showExpiredInspectionExceptions,
			"Stale" => _showStaleInspectionExceptions,
			_ => true
		};

		return result;
	}

	private string GetInspectionExceptionFilterLabel(string status)
	{
		var enabled = status switch
		{
			"Invalid" => _showInvalidInspectionExceptions,
			"ExpiringSoon" => _showExpiringSoonInspectionExceptions,
			"Expired" => _showExpiredInspectionExceptions,
			"Stale" => _showStaleInspectionExceptions,
			_ => true
		};
		var result = (enabled ? "[x] " : "[ ] ") + status;

		return result;
	}
}
