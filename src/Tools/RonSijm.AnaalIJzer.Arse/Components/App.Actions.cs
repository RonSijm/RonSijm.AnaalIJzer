using Microsoft.AspNetCore.Components.Web;
using RonSijm.AnaalIJzer.Arse.FileExtension;
using RonSijm.AnaalIJzer.Application;
using Spectre.Console;

namespace RonSijm.AnaalIJzer.Arse.Components;

public partial class App
{
	private Task OnOperationChanged(string value)
	{
		_selectedOperation = value;
		var operation = ApplicationOperationCatalog.All.SingleOrDefault(candidate => candidate.DisplayName == value);
		_selectedInput = operation is null ? null : ApplicationInputCatalog.Get(operation.DefaultInput).DisplayName;
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

	private static bool IsEscape(KeyboardEventArgs args)
	{
		var result = string.Equals(args.Key, "Escape", StringComparison.OrdinalIgnoreCase)
		             || string.Equals(args.Key, "Esc", StringComparison.OrdinalIgnoreCase);

		return result;
	}

	private static string? NullIfWhiteSpace(string value)
	{
		var result = string.IsNullOrWhiteSpace(value) ? null : value;

		return result;
	}

	private static bool IsMsBuildInput(ApplicationInputKind kind)
	{
		var result = kind is ApplicationInputKind.Project or ApplicationInputKind.Solution;

		return result;
	}
}
