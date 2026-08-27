namespace RonSijm.AnaalIJzer.IntegrationTests.Support;

internal static class ExampleSettingsValidation
{
	public static void ValidateExampleSettingsConfigs(ExampleRepositoryContext context, List<string> failures)
	{
		var settingsPaths = Directory
			.EnumerateFiles(context.ExamplesRoot, "*.anl", SearchOption.AllDirectories)
			.Concat(Directory.EnumerateFiles(context.ExamplesRoot, "*.xml", SearchOption.AllDirectories))
			.OrderBy(path => path, StringComparer.OrdinalIgnoreCase);

		foreach (var settingsPath in settingsPaths)
		{
			var content = File.ReadAllText(settingsPath);
			if (!content.Contains("<ArchitecturalLevels", StringComparison.Ordinal))
			{
				continue;
			}

			var relativePath = Path.GetRelativePath(context.RepositoryRoot, settingsPath);
			ValidateXmlContent(relativePath, content, settingsPath, context.SchemaPath, requireSchemaHint: true, failures);
		}
	}

	public static void ValidateInlineConfigXml(string label, string content, string schemaPath, List<string> failures)
	{
		ValidateXmlContent(label, content, null, schemaPath, requireSchemaHint: false, failures);
	}

	public static string[] FindInlineSettingsSourceFiles(string projectDirectory)
	{
		var result = Directory
			.EnumerateFiles(projectDirectory, "*.cs", SearchOption.AllDirectories)
			.Where(path => !path.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
			.Where(path => !path.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
			.Where(path => File.ReadAllText(path).Contains("AssemblyMetadata(\"AnaalIJzerSettings\"", StringComparison.Ordinal))
			.OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
			.ToArray();

		return result;
	}

	private static void ValidateXmlContent(string label, string content, string? xmlPath, string schemaPath, bool requireSchemaHint, List<string> failures)
	{
		var validationMessages = new List<string>();
		var settings = new System.Xml.XmlReaderSettings
		{
			ValidationType = System.Xml.ValidationType.Schema,
			ValidationFlags = System.Xml.Schema.XmlSchemaValidationFlags.ReportValidationWarnings
		};

		settings.Schemas.Add(string.Empty, schemaPath);
		settings.ValidationEventHandler += (_, args) => validationMessages.Add($"{args.Severity}: {args.Message}");

		try
		{
			using var textReader = new StringReader(content);
			using var reader = System.Xml.XmlReader.Create(textReader, settings);
			reader.MoveToContent();
			var schemaLocation = reader.GetAttribute("noNamespaceSchemaLocation", System.Xml.Schema.XmlSchema.InstanceNamespace);
			if (requireSchemaHint && string.IsNullOrWhiteSpace(schemaLocation))
			{
				failures.Add($"{label}: missing xsi:noNamespaceSchemaLocation schema hint.");
			}
			else if (requireSchemaHint && xmlPath is not null && schemaLocation is not null && !SchemaHintExists(xmlPath, schemaLocation))
			{
				failures.Add($"{label}: schema hint does not resolve: {schemaLocation}");
			}

			while (reader.Read())
			{
			}
		}
		catch (Exception ex)
		{
			failures.Add($"{label}: XML schema validation failed: {ex.Message}");

			return;
		}

		failures.AddRange(validationMessages.Select(message => $"{label}: {message}"));
	}

	private static bool SchemaHintExists(string xmlPath, string schemaLocation)
	{
		if (Uri.TryCreate(schemaLocation, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
		{
			return true;
		}

		var resolvedPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(xmlPath)!, schemaLocation));
		var result = File.Exists(resolvedPath);

		return result;
	}
}
