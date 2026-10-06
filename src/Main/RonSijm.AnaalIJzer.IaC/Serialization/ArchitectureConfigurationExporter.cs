using System.Text;
using System.Xml;

namespace RonSijm.AnaalIJzer.IaC.Serialization;

public static class ArchitectureConfigurationExporter
{
    private static readonly XNamespace XmlSchemaInstance = "http://www.w3.org/2001/XMLSchema-instance";

    public static string Export(ArchitectureConfiguration configuration, ArchitectureConfigurationExportOptions? options = null)
    {
        if (configuration is null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        options ??= new ArchitectureConfigurationExportOptions();

        var document = CreateDocument(configuration, options);
        var builder = new StringBuilder();
        using (var textWriter = new Utf8StringWriter(builder))
        using (var writer = XmlWriter.Create(textWriter, CreateSettings(options, Encoding.UTF8)))
        {
            document.Save(writer);
        }

        var result = builder.ToString();

        return result;
    }

    public static void Save(ArchitectureConfiguration configuration, string path, ArchitectureConfigurationExportOptions? options = null)
    {
        if (configuration is null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("An output path is required.", nameof(path));
        }

        options ??= new ArchitectureConfigurationExportOptions();

        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var document = CreateDocument(configuration, options);
        using var stream = File.Create(fullPath);
        using var writer = XmlWriter.Create(stream, CreateSettings(options, new UTF8Encoding(false)));
        document.Save(writer);
    }

    private static XDocument CreateDocument(ArchitectureConfiguration configuration, ArchitectureConfigurationExportOptions options)
    {
        var root = new XElement("ArchitecturalLevels");
        var writer = new ArchitectureElementWriter(root);
        writer.WriteList("requireRecognizedDependencies", configuration.RequireRecognizedDependencies);
        writer.WriteWhenTrue("enforceAcyclic", configuration.EnforceAcyclic);
        writer.WriteWhenTrue("enforceObservedAcyclic", configuration.EnforceObservedAcyclic);
        writer.WriteWhenTrue("enableReport", configuration.EnableReport);
        writer.WriteOptional("reportPath", configuration.ReportPath);
        writer.WriteWhenTrue("enableDocumentation", configuration.EnableDocumentation);
        writer.WriteOptional("documentationPath", configuration.DocumentationPath);
        writer.WriteOptional("description", configuration.Description);

        if (!string.IsNullOrWhiteSpace(options.SchemaLocation))
        {
            root.SetAttributeValue(XNamespace.Xmlns + "xsi", XmlSchemaInstance.NamespaceName);
            root.SetAttributeValue(XmlSchemaInstance + "noNamespaceSchemaLocation", options.SchemaLocation);
        }

        foreach (var item in configuration.Elements)
        {
            root.Add(CreateElement(item));
        }

        var declaration = options.IncludeXmlDeclaration ? new XDeclaration("1.0", "utf-8", null) : null;
        var result = new XDocument(declaration, root);

        return result;
    }

    private static XElement CreateElement(object value)
    {
        if (value is not ArchitectureElement model)
        {
            throw new InvalidOperationException($"Configuration element '{value.GetType().FullName}' does not derive from {nameof(ArchitectureElement)}.");
        }

        var element = new XElement(model.ElementName);
        var writer = new ArchitectureElementWriter(element);
        model.WriteAttributes(writer);
        foreach (var child in model.GetChildren())
        {
            element.Add(CreateElement(child));
        }

        return element;
    }

    private static XmlWriterSettings CreateSettings(ArchitectureConfigurationExportOptions options, Encoding encoding)
    {
        var result = new XmlWriterSettings
        {
            Encoding = encoding,
            Indent = options.Indent,
            OmitXmlDeclaration = !options.IncludeXmlDeclaration,
            NewLineChars = "\n",
            NewLineHandling = NewLineHandling.Replace
        };

        return result;
    }

    private sealed class Utf8StringWriter(StringBuilder builder) : StringWriter(builder)
    {
        public override Encoding Encoding => Encoding.UTF8;
    }
}
