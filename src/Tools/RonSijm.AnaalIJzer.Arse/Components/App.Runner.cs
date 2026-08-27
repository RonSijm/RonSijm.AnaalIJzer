using System.Collections.Immutable;
using RonSijm.AnaalIJzer.Application;
using RonSijm.AnaalIJzer.Core.Findings;
using Spectre.Console;

namespace RonSijm.AnaalIJzer.Arse.Components;

public partial class App
{
	private async Task RunAsync()
	{
		var operation = CurrentOperation;
		var input = CurrentInput;
		if (_running || operation is null || input is null)
		{
			return;
		}

		_running = true;
		ClearStatus();
		StateHasChanged();

		try
		{
			var generationOptions = CreateGenerationOptions();
			var request = new ApplicationRequest(operation.Kind)
			{
				InputKind = input.Kind,
				InputPaths = operation.SupportsMultipleInputs
					? ApplicationInputPathParser.Parse(_inputPath)
					: NullIfWhiteSpace(_inputPath) is { } inputPath ? [inputPath] : [],
				OutputPath = operation.Kind == ApplicationOperationKind.Inspect ? null : NullIfWhiteSpace(_outputPath),
				Configuration = _configuration,
				GenerationOptions = generationOptions,
				IncludeCodeEvidence = _codeEvidence == IncludeCodeEvidence,
				IncludeDocumentationInput = _inputInclusion == IncludeInput,
				GenerateDocumentation = _generatedDocumentation == GenerateDocumentation,
				Force = _overwrite == Overwrite,
				WriteOutput = operation.Kind != ApplicationOperationKind.Inspect
			};
			var result = await ApplicationRunner.ExecuteAsync(request);
			if (operation.Kind == ApplicationOperationKind.Inspect)
			{
				_inspectionReport = result.Content ?? throw new ApplicationOperationException("Architecture inspection did not return a report.");
				_inspectionFindings = result.Findings;
				_inspectionSummary = result.Message;
				_inspectionColor = result.HasFindings ? Color.Yellow : Color.Green;
				_outputPath = result.OutputPath;
				_selectingInspectionOutput = false;
				ClearStatus();
			}
			else
			{
				SetStatus(result.HasFindings ? "Review needed" : "Complete", result.Message, result.HasFindings ? Color.Yellow : Color.Green);
			}
		}
		catch (ApplicationOperationException ex)
		{
			SetStatus("Cannot run", ex.Message, Color.Yellow);
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

	private Task Clear()
	{
		_selectedOperation = null;
		_selectedInput = null;
		_inputPath = string.Empty;
		_outputPath = string.Empty;
		_configuration = "Release";
		_generationStrategy = SnapshotStrategy;
		_minimumConfidence = "0.90";
		_minimumSupport = "5";
		_codeEvidence = StaticDocumentation;
		_generatedDocumentation = DoNotGenerateDocumentation;
		_inputInclusion = DoNotIncludeInput;
		_overwrite = DoNotOverwrite;
		ClearInspectionResult();
		ClearStatus();

		return Task.CompletedTask;
	}

	private void SetStatus(string title, string message, Color color)
	{
		_statusTitle = title;
		_status = message;
		_statusColor = color;
	}

	private void ClearStatus()
	{
		_statusTitle = string.Empty;
		_status = string.Empty;
		_statusColor = Color.Grey58;
	}

	private void ClearInspectionResult()
	{
		_inspectionReport = null;
		_inspectionFindings = ImmutableArray<ArchitectureFinding>.Empty;
		_inspectionSummary = string.Empty;
		_inspectionColor = Color.Grey58;
		_selectingInspectionOutput = false;
	}
}
