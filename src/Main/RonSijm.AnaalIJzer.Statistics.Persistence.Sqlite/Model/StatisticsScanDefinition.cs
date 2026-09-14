using System.Security.Cryptography;
using System.Text;

namespace RonSijm.AnaalIJzer.Statistics.Persistence.Sqlite.Model;

public sealed class StatisticsScanDefinition(string configuration, string? targetFramework, bool includeGeneratedCode, string collectorVersion)
{
	public string Configuration { get; } = string.IsNullOrWhiteSpace(configuration) ? "Release" : configuration;
	public string? TargetFramework { get; } = string.IsNullOrWhiteSpace(targetFramework) ? null : targetFramework;
	public bool IncludeGeneratedCode { get; } = includeGeneratedCode;
	public string CollectorVersion { get; } = string.IsNullOrWhiteSpace(collectorVersion) ? "1" : collectorVersion;

	public string GetOptionsHash()
	{
		var input = string.Join("\n", Configuration, TargetFramework ?? string.Empty, IncludeGeneratedCode ? "include-generated" : "exclude-generated", CollectorVersion);
		var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
		var result = Convert.ToHexString(hash).ToLowerInvariant();

		return result;
	}
}
