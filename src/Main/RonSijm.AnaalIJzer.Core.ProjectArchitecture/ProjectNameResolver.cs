namespace RonSijm.AnaalIJzer.Core.ProjectArchitecture;

internal static class ProjectNameResolver
{
    internal static string GetProjectName(string projectPath)
    {
        var normalizedPath = projectPath.Trim().Replace('\\', '/');
        var fileNameStart = normalizedPath.LastIndexOf('/');
        var fileName = fileNameStart >= 0 ? normalizedPath.Substring(fileNameStart + 1) : normalizedPath;
        var extensionStart = fileName.LastIndexOf('.');
        var result = extensionStart > 0 ? fileName.Substring(0, extensionStart) : fileName;

        return result;
    }
}