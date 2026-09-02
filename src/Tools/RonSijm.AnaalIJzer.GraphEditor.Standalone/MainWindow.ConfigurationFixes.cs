using System.Collections.Immutable;
using System.IO;
using RonSijm.AnaalIJzer.Application;
using RonSijm.AnaalIJzer.GraphEditor.Wpf.Controls;

namespace RonSijm.AnaalIJzer.GraphEditor.Standalone;

internal sealed partial class MainWindow
{
	private async Task<ArchitectureGraphConfigurationFixCollection> LoadConfigurationFixesAsync(CancellationToken cancellationToken)
	{
		if (!TryCreateFixRequest(ApplicationOperationKind.Fixes, null, out var request, out var message))
		{
			var unavailableResult = new ArchitectureGraphConfigurationFixCollection(message, ImmutableArray<ArchitectureGraphConfigurationFixProposal>.Empty);

			return unavailableResult;
		}

		var runner = new ApplicationRunner();
		var result = await runner.ExecuteAsync(request, cancellationToken);
		var proposals = result.FixProposals
			.Select(proposal => new ArchitectureGraphConfigurationFixProposal(
				proposal.Id,
				proposal.Title,
				proposal.DiagnosticMessage,
				proposal.Risk,
				proposal.DiagnosticId,
				proposal.TargetPath,
				proposal.PreviewDiff,
				proposal.DiagnosticProperties))
			.ToImmutableArray();
		var collection = new ArchitectureGraphConfigurationFixCollection(result.Message, proposals);

		return collection;
	}

	private async Task<ArchitectureGraphConfigurationFixApplyResult> ApplyConfigurationFixAsync(string fixId, CancellationToken cancellationToken)
	{
		if (!TryCreateFixRequest(ApplicationOperationKind.ApplyFix, fixId, out var request, out var message))
		{
			throw new InvalidOperationException(message);
		}

		var runner = new ApplicationRunner();
		var result = await runner.ExecuteAsync(request, cancellationToken);
		var applyResult = new ArchitectureGraphConfigurationFixApplyResult(result.Message);

		return applyResult;
	}

	private bool TryCreateFixRequest(ApplicationOperationKind operation, string? fixId, out ApplicationRequest request, out string message)
	{
		request = new ApplicationRequest(operation);
		var inputPath = _pathBox.Text;
		if (string.IsNullOrWhiteSpace(inputPath))
		{
			message = "Choose a project or solution before loading configuration fixes.";

			return false;
		}

		var fullPath = Path.GetFullPath(inputPath);
		if (string.Equals(Path.GetExtension(fullPath), ".csproj", StringComparison.OrdinalIgnoreCase))
		{
			request = new ApplicationRequest(operation)
			{
				InputKind = ApplicationInputKind.Project,
				InputPaths = [fullPath],
				WriteOutput = false,
				FixId = fixId
			};
			message = string.Empty;

			return true;
		}

		if (IsSolutionExtension(Path.GetExtension(fullPath)))
		{
			request = new ApplicationRequest(operation)
			{
				InputKind = ApplicationInputKind.Solution,
				InputPaths = [fullPath],
				WriteOutput = false,
				FixId = fixId
			};
			message = string.Empty;

			return true;
		}

		message = "Configuration fixes are available when the editor is opened from a project or solution.";

		return false;
	}

	private static bool IsSolutionExtension(string extension)
	{
		var result = string.Equals(extension, ".sln", StringComparison.OrdinalIgnoreCase)
		             || string.Equals(extension, ".slnx", StringComparison.OrdinalIgnoreCase);

		return result;
	}
}
