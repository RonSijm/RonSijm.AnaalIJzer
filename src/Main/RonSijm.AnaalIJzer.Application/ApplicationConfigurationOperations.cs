using System.Text;
using RonSijm.AnaalIJzer.ConfigurationEditing.Document;
using RonSijm.AnaalIJzer.Documentation;

namespace RonSijm.AnaalIJzer.Application;

internal static partial class ApplicationConfigurationOperations
{
	public static async Task<ApplicationRunResult> GenerateConfigAsync(ApplicationRequest request, ApplicationWorkspaceAnalysisService workspace, CancellationToken cancellationToken)
	{
		if (request.InputKind == ApplicationInputKind.Solution)
		{
			var solutionResult = await GenerateSolutionConfigAsync(request, workspace, cancellationToken);

			return solutionResult;
		}

		var result = await GenerateProjectConfigAsync(request, workspace, cancellationToken);

		return result;
	}
}

