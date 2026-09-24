namespace RonSijm.AnaalIJzer.Workspace.Loading;

/// <summary>
/// Initializes the installed MSBuild environment before a host creates an <c>MSBuildWorkspace</c>.
/// </summary>
public static class WorkspaceBuildEnvironment
{
    public static void Initialize()
    {
        Environment.SetEnvironmentVariable("MSBUILDDISABLENODEREUSE", "1");
        WorkspaceBuildRegistration.EnsureRegistered();
        WorkspaceBuildHostCompatibility.EnsureCompatible(AppContext.BaseDirectory);
    }
}