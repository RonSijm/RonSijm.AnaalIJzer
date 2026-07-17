// ReSharper disable All - Justification: Example File
using System.Threading;
using System.Threading.Tasks;

namespace Example.CascadingDependencyRules;

public interface ISsoManager
{
    Task<string> SignInAsync(CancellationToken cancellationToken);

    int? GetRetryCount();
}

public interface ISsoRepository
{
    Task<int?> CountActiveSessionsAsync(CancellationToken cancellationToken);
}