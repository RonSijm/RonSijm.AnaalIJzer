using Microsoft.Build.Locator;

namespace RonSijm.AnaalIJzer.Workspace.Loading;

internal static class WorkspaceBuildRegistration
{
    private static readonly object RegistrationLock = new();
    private static string? _dotNetRoot;

    internal static string? DotNetRoot => _dotNetRoot;

    public static void EnsureRegistered()
    {
        lock (RegistrationLock)
        {
            if (MSBuildLocator.CanRegister)
            {
                var dotNetSdk = MSBuildLocator.QueryVisualStudioInstances()
                    .Where(instance => instance.DiscoveryType == DiscoveryType.DotNetSdk)
                    .OrderByDescending(instance => instance.Version)
                    .FirstOrDefault();
                if (dotNetSdk is not null)
                {
                    _dotNetRoot = Path.GetFullPath(Path.Combine(dotNetSdk.MSBuildPath, "..", ".."));
                    MSBuildLocator.RegisterInstance(dotNetSdk);

                    return;
                }

                MSBuildLocator.RegisterDefaults();
            }
        }
    }
}