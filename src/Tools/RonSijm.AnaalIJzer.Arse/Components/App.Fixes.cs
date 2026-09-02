using System.Text;
using Microsoft.AspNetCore.Components.Web;
using RonSijm.AnaalIJzer.Application;
using Spectre.Console;

namespace RonSijm.AnaalIJzer.Arse.Components;

public partial class App
{
	private string[] FixProposalOptions
	{
		get
		{
			var result = _fixProposals.Select(proposal => proposal.Id).ToArray();

			return result;
		}
	}

	private Task OnFixProposalChanged(string value)
	{
		_selectedFixProposal = value;
		ClearStatus();

		return Task.CompletedTask;
	}

	private async Task OnFixReportKeyDown(KeyboardEventArgs args, Func<KeyboardEventArgs, Task> scroll)
	{
		if (IsEscape(args))
		{
			await ReturnToFixForm();

			return;
		}

		await scroll(args);
	}

	private Task OnFixResultKeyUp(KeyboardEventArgs args)
	{
		var result = IsEscape(args) ? ReturnToFixForm() : Task.CompletedTask;

		return result;
	}

	private Task OnFixSaveKey(KeyboardEventArgs args)
	{
		var result = IsEscape(args) ? CancelFixSave() : Task.CompletedTask;

		return result;
	}

	private Task ShowFixSave()
	{
		_selectingFixOutput = true;
		ClearStatus();

		return Task.CompletedTask;
	}

	private Task CancelFixSave()
	{
		_selectingFixOutput = false;
		ClearStatus();

		return Task.CompletedTask;
	}

	private Task ReturnToFixForm()
	{
		ClearFixResult();
		ClearStatus();

		return Task.CompletedTask;
	}

	private async Task SaveFixesAsync()
	{
		if (_fixReport is null)
		{
			return;
		}

		try
		{
			var outputPath = NullIfWhiteSpace(_outputPath) is { } path
				? Path.GetFullPath(path)
				: throw new ApplicationOperationException("Select an output file.");
			Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
			await File.WriteAllTextAsync(outputPath, _fixReport, new UTF8Encoding(false));
			_outputPath = outputPath;
			_selectingFixOutput = false;
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

	private async Task ApplySelectedFixAsync()
	{
		var input = CurrentInput;
		if (_running || input is null || string.IsNullOrWhiteSpace(_selectedFixProposal))
		{
			return;
		}

		try
		{
			_running = true;
			ClearStatus();
			StateHasChanged();

			var request = new ApplicationRequest(ApplicationOperationKind.ApplyFix)
			{
				InputKind = input.Kind,
				InputPaths = NullIfWhiteSpace(_inputPath) is { } inputPath ? [inputPath] : [],
				FixId = _selectedFixProposal,
				Configuration = _configuration,
				WriteOutput = false
			};
			var result = await ApplicationRunner.ExecuteAsync(request);
			_fixReport = result.Content ?? _fixReport;
			_fixProposals = result.FixProposals;
			_selectedFixProposal = _fixProposals.FirstOrDefault()?.Id;
			_outputPath = result.OutputPath;
			_selectingFixOutput = false;
			SetStatus(result.HasFindings ? "Review needed" : "Complete", result.Message, result.HasFindings ? Color.Yellow : Color.Green);
		}
		catch (ApplicationOperationException ex)
		{
			SetStatus("Cannot apply", ex.Message, Color.Yellow);
		}
		catch (Exception ex)
		{
			SetStatus("Failed", ex.Message, Color.Red);
		}
		finally
		{
			_running = false;
			StateHasChanged();
		}
	}
}
