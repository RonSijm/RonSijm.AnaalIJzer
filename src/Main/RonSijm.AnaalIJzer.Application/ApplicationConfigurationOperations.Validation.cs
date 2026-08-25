namespace RonSijm.AnaalIJzer.Application;

internal static partial class ApplicationConfigurationOperations
{
	private static void EnsureSchemaOutputDoesNotCollide(string outputPath, string schemaPath)
	{
		if (string.Equals(outputPath, schemaPath, StringComparison.OrdinalIgnoreCase))
		{
			throw new ApplicationOperationException("The configuration output path may not be AnaalIJzer.xsd.");
		}
	}

	private static void EnsureOutputIsWritable(string outputPath, bool force)
	{
		if (File.Exists(outputPath) && !force)
		{
			throw new ApplicationOperationException($"Output already exists: {outputPath}. Enable overwrite to replace it.");
		}
	}

	private static void EnsureDocumentationOutputIsWritable(bool generateDocumentation, string documentationPath, bool force)
	{
		if (generateDocumentation && File.Exists(documentationPath) && !force)
		{
			throw new ApplicationOperationException($"Output already exists: {documentationPath}. Enable overwrite to replace it.");
		}
	}

	private static async Task EnsureSchemaFileCanBeReplacedAsync(string schemaPath, string schema, bool force, CancellationToken cancellationToken)
	{
		if (File.Exists(schemaPath)
		    && !force
		    && !string.Equals(
			    ApplicationOutputPathService.NormalizeLineEndings(await File.ReadAllTextAsync(schemaPath, cancellationToken)),
			    ApplicationOutputPathService.NormalizeLineEndings(schema),
			    StringComparison.Ordinal))
		{
			throw new ApplicationOperationException($"A different schema already exists at {schemaPath}. Enable overwrite to replace it.");
		}
	}

	private static void EnsureGeneratedConfigurationIsValid(IReadOnlyList<object> generatedDiagnostics)
	{
		if (generatedDiagnostics.Count == 0)
		{
			return;
		}

		var diagnosticSummary = string.Join(Environment.NewLine, generatedDiagnostics.Take(10).Select(diagnostic => diagnostic.ToString()));
		throw new ApplicationOperationException($"The inferred configuration did not cover the existing architecture:{Environment.NewLine}{diagnosticSummary}");
	}
}
