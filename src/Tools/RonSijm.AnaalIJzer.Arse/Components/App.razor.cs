using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Components.Web;
using RonSijm.AnaalIJzer.Arse.FileExtension;
using RonSijm.AnaalIJzer.Tooling;
using Spectre.Console;

namespace RonSijm.AnaalIJzer.Arse.Components;

public partial class App
{
	private const string DoNotOverwrite = "Do not overwrite";
	private const string Overwrite = "Overwrite";
	private const string SnapshotStrategy = "Snapshot";
	private const string HelpfulStrategy = "Helpful baseline";
	private const string ConventionsStrategy = "Infer conventions";
	private const string StaticDocumentation = "XML rules only";
	private const string IncludeCodeEvidence = "Include code evidence";
	private const string DoNotGenerateDocumentation = "Do not generate documentation";
	private const string GenerateDocumentation = "Generate documentation";
	private const string DoNotIncludeInput = "Do not include input XML";
	private const string IncludeInput = "Include input XML";
	private const string AssociateAnlFiles = "Associate .anl files with Arse";
	private const string UnassociateAnlFiles = "Unassociate .anl files from Arse";

	private static readonly string[] OperationOptions = [..ToolOperationCatalog.All.Select(operation => operation.DisplayName), AssociateAnlFiles, UnassociateAnlFiles];
	private static readonly string[] OverwriteOptions = [DoNotOverwrite, Overwrite];
	private static readonly string[] GenerationStrategyOptions = [SnapshotStrategy, HelpfulStrategy, ConventionsStrategy];
	private static readonly string[] CodeEvidenceOptions = [StaticDocumentation, IncludeCodeEvidence];
	private static readonly string[] GeneratedDocumentationOptions = [DoNotGenerateDocumentation, GenerateDocumentation];
	private static readonly string[] InputInclusionOptions = [DoNotIncludeInput, IncludeInput];
	private static readonly string[] ProjectFileExtensions = [".csproj"];
	private static readonly string[] SolutionFileExtensions = [".sln", ".slnx"];
	private static readonly string[] ArchitectureConfigFileExtensions = [".anl", ".xml"];
	private static readonly string[] MarkdownFileExtensions = [".md"];

	private string? _selectedOperation;
	private string? _selectedInput;
	private string _inputPath = string.Empty;
	private string _outputPath = string.Empty;
	private string _configuration = "Release";
	private string _generationStrategy = SnapshotStrategy;
	private string _minimumConfidence = "0.90";
	private string _minimumSupport = "5";
	private string _codeEvidence = StaticDocumentation;
	private string _generatedDocumentation = DoNotGenerateDocumentation;
	private string _inputInclusion = DoNotIncludeInput;
	private string _overwrite = DoNotOverwrite;
	private string _status = string.Empty;
	private string _statusTitle = string.Empty;
	private Color _statusColor = Color.Grey58;
	private string? _inspectionReport;
	private string _inspectionSummary = string.Empty;
	private Color _inspectionColor = Color.Grey58;
	private bool _selectingInspectionOutput;
	private bool _running;

	private ToolOperationDefinition? CurrentOperation
    {
        get { return _selectedOperation is null ? null : ToolOperationCatalog.All.SingleOrDefault(operation => operation.DisplayName == _selectedOperation); }
    }

	private bool IsFileAssociationOperation
	{
		get
		{
			var result = _selectedOperation is AssociateAnlFiles or UnassociateAnlFiles;

			return result;
		}
	}

    private ToolInputDefinition? CurrentInput
    {
        get { return _selectedInput is null ? null : ToolInputCatalog.All.Single(input => input.DisplayName == _selectedInput); }
    }

    private string[] CurrentInputOptions
    {
        get { return CurrentOperation?.SupportedInputs.Select(kind => ToolInputCatalog.Get(kind).DisplayName).ToArray() ?? []; }
    }

    private IReadOnlyCollection<string> CurrentInputFileExtensions
    {
        get
        {
            return CurrentInput?.Kind switch
            {
                ToolInputKind.Project => ProjectFileExtensions,
                ToolInputKind.Solution => SolutionFileExtensions,
                _ => ArchitectureConfigFileExtensions
            };
        }
    }

    private IReadOnlyCollection<string> CurrentOutputFileExtensions
    {
        get
        {
            return CurrentOperation?.Kind switch
            {
                ToolOperationKind.GenerateConfig or ToolOperationKind.ExportConfig or ToolOperationKind.MergeConfig => ArchitectureConfigFileExtensions,
                ToolOperationKind.Documentation or ToolOperationKind.Report or ToolOperationKind.Inspect => MarkdownFileExtensions,
                _ => []
            };
        }
    }

    private Task OnOperationChanged(string value)
	{
		_selectedOperation = value;
		var operation = ToolOperationCatalog.All.SingleOrDefault(candidate => candidate.DisplayName == value);
		_selectedInput = operation is null ? null : ToolInputCatalog.Get(operation.DefaultInput).DisplayName;
		_inputPath = string.Empty;
		_outputPath = string.Empty;
		_generationStrategy = SnapshotStrategy;
		_minimumConfidence = "0.90";
		_minimumSupport = "5";
		_codeEvidence = StaticDocumentation;
		_generatedDocumentation = DoNotGenerateDocumentation;
		_inputInclusion = DoNotIncludeInput;
		ClearInspectionResult();
		ClearStatus();
		return Task.CompletedTask;
	}

	private Task RunFileAssociationAsync()
	{
		try
		{
			var result = _selectedOperation == AssociateAnlFiles
				? ArseFileAssociation.AssociateAnlFiles()
				: ArseFileAssociation.UnassociateAnlFiles();
			SetStatus(result.Changed ? "Complete" : "No change", result.Message, Color.Green);
		}
		catch (Exception ex)
		{
			SetStatus("Failed", ex.Message, Color.Red);
		}

		return Task.CompletedTask;
	}

	private Task OnInputChanged(string value)
	{
		_selectedInput = value;
		_inputPath = string.Empty;
		_outputPath = string.Empty;
		_codeEvidence = StaticDocumentation;
		ClearInspectionResult();
		ClearStatus();
		return Task.CompletedTask;
	}

	private Task OnInputPathChanged(string? value)
	{
		_inputPath = value ?? string.Empty;
		ClearStatus();
		return Task.CompletedTask;
	}

	private Task OnOutputPathChanged(string? value)
	{
		_outputPath = value ?? string.Empty;
		ClearStatus();
		return Task.CompletedTask;
	}

	private Task OnConfigurationChanged(string? value)
	{
		_configuration = string.IsNullOrWhiteSpace(value) ? "Release" : value;
		ClearStatus();
		return Task.CompletedTask;
	}

	private Task OnOverwriteChanged(string value)
	{
		_overwrite = value;
		ClearStatus();
		return Task.CompletedTask;
	}

	private Task OnGenerationStrategyChanged(string value)
	{
		_generationStrategy = value;
		ClearStatus();
		return Task.CompletedTask;
	}

	private Task OnMinimumConfidenceChanged(string? value)
	{
		_minimumConfidence = value ?? string.Empty;
		ClearStatus();
		return Task.CompletedTask;
	}

	private Task OnMinimumSupportChanged(string? value)
	{
		_minimumSupport = value ?? string.Empty;
		ClearStatus();
		return Task.CompletedTask;
	}

	private Task OnCodeEvidenceChanged(string value)
	{
		_codeEvidence = value;
		ClearStatus();
		return Task.CompletedTask;
	}

	private Task OnGeneratedDocumentationChanged(string value)
	{
		_generatedDocumentation = value;
		ClearStatus();
		return Task.CompletedTask;
	}

	private Task OnInputInclusionChanged(string value)
	{
		_inputInclusion = value;
		ClearStatus();
		return Task.CompletedTask;
	}

	private Task OnFormKeyUp(KeyboardEventArgs args)
	{
		if (IsEscape(args))
		{
			return Clear();
		}

		return Task.CompletedTask;
	}

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
        return IsEscape(args) ? ReturnToInspectionForm() : Task.CompletedTask;
    }

    private Task OnInspectionSaveKey(KeyboardEventArgs args)
    {
        return IsEscape(args) ? CancelInspectionSave() : Task.CompletedTask;
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
				: throw new ToolingException("Select an output file.");
			Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
			await File.WriteAllTextAsync(outputPath, _inspectionReport, new UTF8Encoding(false));
			_outputPath = outputPath;
			_selectingInspectionOutput = false;
			SetStatus("Saved", $"Wrote {outputPath}", Color.Green);
		}
		catch (ToolingException ex)
		{
			SetStatus("Cannot save", ex.Message, Color.Yellow);
		}
		catch (Exception ex)
		{
			SetStatus("Cannot save", ex.Message, Color.Red);
		}
	}

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
			var request = new ToolRequest(operation.Kind)
			{
				InputKind = input.Kind,
				InputPaths = operation.SupportsMultipleInputs
					? ToolInputPathParser.Parse(_inputPath)
					: NullIfWhiteSpace(_inputPath) is { } inputPath ? [inputPath] : [],
				OutputPath = operation.Kind == ToolOperationKind.Inspect ? null : NullIfWhiteSpace(_outputPath),
				Configuration = _configuration,
				GenerationOptions = generationOptions,
				IncludeCodeEvidence = _codeEvidence == IncludeCodeEvidence,
				IncludeDocumentationInput = _inputInclusion == IncludeInput,
				GenerateDocumentation = _generatedDocumentation == GenerateDocumentation,
				Force = _overwrite == Overwrite,
				WriteOutput = operation.Kind != ToolOperationKind.Inspect
			};
			var result = await ToolRunner.ExecuteAsync(request);
			if (operation.Kind == ToolOperationKind.Inspect)
			{
				_inspectionReport = result.Content ?? throw new ToolingException("Architecture inspection did not return a report.");
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
		catch (ToolingException ex)
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
		_inspectionSummary = string.Empty;
		_inspectionColor = Color.Grey58;
		_selectingInspectionOutput = false;
	}

	private static bool IsEscape(KeyboardEventArgs args)
    {
        return string.Equals(args.Key, "Escape", StringComparison.OrdinalIgnoreCase)
               || string.Equals(args.Key, "Esc", StringComparison.OrdinalIgnoreCase);
    }

    private static string? NullIfWhiteSpace(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

	private static bool IsMsBuildInput(ToolInputKind kind)
	{
		var result = kind is ToolInputKind.Project or ToolInputKind.Solution;

		return result;
	}

	private ConfigurationGenerationOptions CreateGenerationOptions()
	{
		if (_generationStrategy == SnapshotStrategy)
		{
			return new();
		}

		if (_generationStrategy == HelpfulStrategy)
		{
			return new ConfigurationGenerationOptions
			{
				Strategy = ConfigurationGenerationStrategy.Helpful
			};
		}

		if (!double.TryParse(_minimumConfidence, NumberStyles.Float, CultureInfo.InvariantCulture, out var minimumConfidence))
		{
			throw new ToolingException("Minimum confidence must be a number from 0 to 1.");
		}

		if (!int.TryParse(_minimumSupport, NumberStyles.None, CultureInfo.InvariantCulture, out var minimumSupport))
		{
			throw new ToolingException("Minimum supporting callers must be a whole number.");
		}

		return new ConfigurationGenerationOptions
		{
			Strategy = ConfigurationGenerationStrategy.Conventions,
			MinimumConfidence = minimumConfidence,
			MinimumSupport = minimumSupport
		};
	}
}
