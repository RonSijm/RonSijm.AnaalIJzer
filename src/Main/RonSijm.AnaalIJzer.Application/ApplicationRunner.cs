namespace RonSijm.AnaalIJzer.Application;

public sealed class ApplicationRunner
{
	public async Task<ApplicationRunResult> ExecuteAsync(ApplicationRequest request, CancellationToken cancellationToken = default)
	{
		var operation = ApplicationOperationCatalog.Get(request.Operation);
		ApplicationRequestValidator.Validate(request, operation);
		var workspace = new ApplicationWorkspaceAnalysisService(request.Configuration);

		var result = request.Operation switch
		{
			ApplicationOperationKind.GenerateConfig => await ApplicationConfigurationOperations.GenerateConfigAsync(request, workspace, cancellationToken),
			ApplicationOperationKind.ExportConfig => await ApplicationConfigurationOperations.ExportConfigAsync(request, workspace, cancellationToken),
			ApplicationOperationKind.Documentation => await ApplicationDocumentationOperations.GenerateDocumentationAsync(request, workspace, cancellationToken),
			ApplicationOperationKind.Report => await ApplicationDocumentationOperations.GenerateReportAsync(request, workspace, cancellationToken),
			ApplicationOperationKind.Inspect => await ApplicationInspectionOperations.InspectArchitectureAsync(request, workspace, cancellationToken),
			ApplicationOperationKind.MergeConfig => await ApplicationConfigurationFileOperations.MergeConfigAsync(request, cancellationToken),
			ApplicationOperationKind.SplitConfig => await ApplicationConfigurationFileOperations.SplitConfigAsync(request, cancellationToken),
			ApplicationOperationKind.FormatConfig => await ApplicationConfigurationFileOperations.FormatConfigAsync(request, cancellationToken),
			ApplicationOperationKind.ExplainConfig => await ApplicationConfigurationFileOperations.ExplainConfigAsync(request, cancellationToken),
			_ => throw new ApplicationOperationException($"Unsupported operation: {request.Operation}")
		};

		return result;
	}
}

