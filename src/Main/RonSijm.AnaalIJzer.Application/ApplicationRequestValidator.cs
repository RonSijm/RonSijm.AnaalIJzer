namespace RonSijm.AnaalIJzer.Application;

internal static class ApplicationRequestValidator
{
	public static void Validate(ApplicationRequest request, ApplicationOperationDefinition operation)
	{
		if (request.InputKind is null || request.InputPaths.Count == 0 || request.InputPaths.Any(string.IsNullOrWhiteSpace))
		{
			throw new ApplicationOperationException(operation.SupportedInputs.Count > 1
				? "Select a project or an architecture settings file."
				: $"Select {ApplicationInputCatalog.Get(operation.DefaultInput).DisplayName.ToLowerInvariant()} input.");
		}

		if (!operation.Supports(request.InputKind.Value))
		{
			var input = ApplicationInputCatalog.Get(request.InputKind.Value);
			throw new ApplicationOperationException($"{operation.DisplayName} does not support {input.DisplayName.ToLowerInvariant()} input.");
		}

		if (request.InputPaths.Count < operation.MinimumInputCount)
		{
			throw new ApplicationOperationException($"{operation.DisplayName} requires at least {operation.MinimumInputCount} input files.");
		}

		if (operation.MaximumInputCount is { } maximumInputCount && request.InputPaths.Count > maximumInputCount)
		{
			throw new ApplicationOperationException($"{operation.DisplayName} accepts at most {maximumInputCount} input file(s).");
		}

		if (request.GenerationOptions.MinimumConfidence is <= 0 or > 1)
		{
			throw new ApplicationOperationException("Minimum confidence must be greater than 0 and no greater than 1.");
		}

		if (request.GenerationOptions.MinimumSupport < 1)
		{
			throw new ApplicationOperationException("Minimum support must be at least 1.");
		}

		if (request.IncludeCodeEvidence && (request.Operation != ApplicationOperationKind.Documentation || request.InputKind != ApplicationInputKind.Project))
		{
			throw new ApplicationOperationException("Code evidence is available only when generating documentation from a project.");
		}

		if (request.GenerateDocumentation && request.Operation != ApplicationOperationKind.GenerateConfig)
		{
			throw new ApplicationOperationException("Automatic documentation is available only when generating a configuration.");
		}

		if (request.IncludeDocumentationInput
		    && request.Operation != ApplicationOperationKind.Documentation
		    && !(request.Operation == ApplicationOperationKind.GenerateConfig && request.GenerateDocumentation))
		{
			throw new ApplicationOperationException("Input XML can be included only in generated documentation.");
		}

		if (!request.WriteOutput && request.Operation != ApplicationOperationKind.Inspect)
		{
			throw new ApplicationOperationException("Preview without writing output is available only for architecture inspection.");
		}
	}
}

