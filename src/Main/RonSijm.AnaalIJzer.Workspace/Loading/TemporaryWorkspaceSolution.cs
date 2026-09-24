using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace RonSijm.AnaalIJzer.Workspace.Loading;

internal sealed class TemporaryWorkspaceSolution : IDisposable
{
    private TemporaryWorkspaceSolution(string directoryPath, string filePath)
    {
        DirectoryPath = directoryPath;
        FilePath = filePath;
    }

    private string DirectoryPath { get; }
    public string FilePath { get; }

    public static TemporaryWorkspaceSolution Create(IReadOnlyCollection<string> projectPaths)
    {
        var directoryPath = Path.Combine(Path.GetTempPath(), "AnaalIJzer", "Workspace", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directoryPath);
        var filePath = Path.Combine(directoryPath, "Projects.slnx");
        var document = new XDocument(
            new XElement("Solution",
                projectPaths.Select(projectPath => new XElement("Project", new XAttribute("Path", projectPath)))));
        var settings = new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(false),
            Indent = true
        };
        using (var writer = XmlWriter.Create(filePath, settings))
        {
            document.Save(writer);
        }

        var result = new TemporaryWorkspaceSolution(directoryPath, filePath);

        return result;
    }

    public void Dispose()
    {
        if (File.Exists(FilePath))
        {
            File.Delete(FilePath);
        }

        if (Directory.Exists(DirectoryPath))
        {
            Directory.Delete(DirectoryPath);
        }
    }
}