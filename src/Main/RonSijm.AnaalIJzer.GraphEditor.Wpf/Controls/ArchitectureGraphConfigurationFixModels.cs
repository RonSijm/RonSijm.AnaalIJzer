using System.Collections.Immutable;

namespace RonSijm.AnaalIJzer.GraphEditor.Wpf.Controls;

public sealed class ArchitectureGraphConfigurationFixProposal
{
	public ArchitectureGraphConfigurationFixProposal(
		string id,
		string title,
		string summary,
		string risk,
		string diagnosticId,
		string targetPath,
		string previewDiff,
		ImmutableDictionary<string, string>? diagnosticProperties = null)
	{
		Id = id;
		Title = title;
		Summary = summary;
		Risk = risk;
		DiagnosticId = diagnosticId;
		TargetPath = targetPath;
		PreviewDiff = previewDiff;
		DiagnosticProperties = diagnosticProperties ?? ImmutableDictionary<string, string>.Empty;
	}

	public string Id { get; }

	public string Title { get; }

	public string Summary { get; }

	public string Risk { get; }

	public string DiagnosticId { get; }

	public string TargetPath { get; }

	public string PreviewDiff { get; }

	public ImmutableDictionary<string, string> DiagnosticProperties { get; }
}

public sealed class ArchitectureGraphConfigurationFixCollection
{
	public ArchitectureGraphConfigurationFixCollection(string message, ImmutableArray<ArchitectureGraphConfigurationFixProposal> proposals)
	{
		Message = message;
		Proposals = proposals;
	}

	public string Message { get; }

	public ImmutableArray<ArchitectureGraphConfigurationFixProposal> Proposals { get; }

	public static ArchitectureGraphConfigurationFixCollection Empty { get; } =
		new(string.Empty, ImmutableArray<ArchitectureGraphConfigurationFixProposal>.Empty);
}

public sealed class ArchitectureGraphConfigurationFixApplyResult
{
	public ArchitectureGraphConfigurationFixApplyResult(string message)
	{
		Message = message;
	}

	public string Message { get; }
}
