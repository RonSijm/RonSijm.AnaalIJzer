using System.Collections.Immutable;
using Microsoft.AspNetCore.Components;
using RonSijm.AnaalIJzer.Application;
using RonSijm.AnaalIJzer.Core.Findings;
using Spectre.Console;

namespace RonSijm.AnaalIJzer.Arse.Components;

public partial class App : ComponentBase
{
	private const string DoNotOverwrite = "Do not overwrite";
	private const string Overwrite = "Overwrite";
	private const string SnapshotStrategy = "Snapshot";
	private const string HelpfulStrategy = "Helpful baseline";
	private const string ConventionsStrategy = "Infer conventions";
	private const string StaticDocumentation = "Settings only";
	private const string IncludeCodeEvidence = "Include code evidence";
	private const string DoNotGenerateDocumentation = "Do not generate documentation";
	private const string GenerateDocumentation = "Generate documentation";
	private const string DoNotIncludeInput = "Do not include input settings";
	private const string IncludeInput = "Include input settings";
	private const string AssociateAnlFiles = "Associate .anl files with Arse";
	private const string UnassociateAnlFiles = "Unassociate .anl files from Arse";

	private static readonly string[] OperationOptions = [..ApplicationOperationCatalog.All.Where(operation => operation.Kind != ApplicationOperationKind.ApplyFix).Select(operation => operation.DisplayName), AssociateAnlFiles, UnassociateAnlFiles];
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
	private ImmutableArray<ArchitectureFinding> _inspectionFindings = ImmutableArray<ArchitectureFinding>.Empty;
	private string? _fixReport;
	private ImmutableArray<ApplicationConfigurationFixProposal> _fixProposals = ImmutableArray<ApplicationConfigurationFixProposal>.Empty;
	private string? _selectedFixProposal;
	private bool _selectingFixOutput;
	private string _inspectionSummary = string.Empty;
	private Color _inspectionColor = Color.Grey58;
	private bool _selectingInspectionOutput;
	private bool _running;
	private bool _showInvalidInspectionExceptions = true;
	private bool _showExpiringSoonInspectionExceptions = true;
	private bool _showExpiredInspectionExceptions = true;
	private bool _showStaleInspectionExceptions = true;

	[Inject]
	private ApplicationRunner ApplicationRunner { get; set; } = null!;

	private ApplicationOperationDefinition? CurrentOperation
	{
		get { return _selectedOperation is null ? null : ApplicationOperationCatalog.All.SingleOrDefault(operation => operation.DisplayName == _selectedOperation); }
	}

	private bool IsFileAssociationOperation
	{
		get
		{
			var result = _selectedOperation is AssociateAnlFiles or UnassociateAnlFiles;

			return result;
		}
	}

	private ApplicationInputDefinition? CurrentInput
	{
		get { return _selectedInput is null ? null : ApplicationInputCatalog.All.Single(input => input.DisplayName == _selectedInput); }
	}

	private string[] CurrentInputOptions
	{
		get { return CurrentOperation?.SupportedInputs.Select(kind => ApplicationInputCatalog.Get(kind).DisplayName).ToArray() ?? []; }
	}

	private IReadOnlyCollection<string> CurrentInputFileExtensions
	{
		get
		{
			return CurrentInput?.Kind switch
			{
				ApplicationInputKind.Project => ProjectFileExtensions,
				ApplicationInputKind.Solution => SolutionFileExtensions,
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
				ApplicationOperationKind.GenerateConfig or ApplicationOperationKind.ExportConfig or ApplicationOperationKind.MergeConfig or ApplicationOperationKind.FormatConfig => ArchitectureConfigFileExtensions,
				ApplicationOperationKind.Documentation or ApplicationOperationKind.Report or ApplicationOperationKind.Inspect or ApplicationOperationKind.ExplainConfig or ApplicationOperationKind.Fixes => MarkdownFileExtensions,
				_ => []
			};
		}
	}
}
