using System.Collections.Immutable;

namespace RonSijm.AnaalIJzer.Core.ProjectArchitecture.SolutionTopology;

public static class SolutionTopologyCycleDetector
{
	public static ImmutableArray<SolutionTopologyCycle> FindConfiguredCycles(SolutionTopologyConfig config)
	{
		if (!config.EnforceAcyclic)
		{
			return ImmutableArray<SolutionTopologyCycle>.Empty;
		}

		var allowedRules = config.Rules
			.Where(rule => rule.Kind == SolutionModuleReferenceRuleKind.Allowed && rule.From != "*" && rule.To != "*" && rule.From != rule.To)
			.ToImmutableArray();
		var rulesBySource = allowedRules
			.GroupBy(rule => rule.From, StringComparer.Ordinal)
			.ToDictionary(group => group.Key, group => group.OrderBy(rule => rule.To, StringComparer.Ordinal).ToImmutableArray(), StringComparer.Ordinal);
		var cycles = ImmutableArray.CreateBuilder<SolutionTopologyCycle>();
		var seenCycleKeys = new HashSet<string>(StringComparer.Ordinal);

		foreach (var module in config.Modules.OrderBy(module => module.Name, StringComparer.Ordinal))
		{
			FindCyclesFrom(module.Name, module.Name, rulesBySource, [], [], cycles, seenCycleKeys);
		}

		var result = cycles.ToImmutable();

		return result;
	}

	private static void FindCyclesFrom(
		string startModule,
		string currentModule,
		IReadOnlyDictionary<string, ImmutableArray<SolutionModuleReferenceRule>> rulesBySource,
		List<string> pathModules,
		List<SolutionModuleReferenceRule> pathRules,
		ImmutableArray<SolutionTopologyCycle>.Builder cycles,
		HashSet<string> seenCycleKeys)
	{
		pathModules.Add(currentModule);
		if (rulesBySource.TryGetValue(currentModule, out var outgoingRules))
		{
			foreach (var rule in outgoingRules)
			{
				if (rule.To == startModule && pathModules.Count > 1)
				{
					var cycleModules = pathModules.ToImmutableArray();
					var cycleRules = pathRules.Append(rule).ToImmutableArray();
					var canonicalKey = CreateCanonicalKey(cycleModules);
					if (seenCycleKeys.Add(canonicalKey))
					{
						cycles.Add(new SolutionTopologyCycle(cycleModules, cycleRules));
					}

					continue;
				}

				if (pathModules.Contains(rule.To, StringComparer.Ordinal))
				{
					continue;
				}

				pathRules.Add(rule);
				FindCyclesFrom(startModule, rule.To, rulesBySource, pathModules, pathRules, cycles, seenCycleKeys);
				pathRules.RemoveAt(pathRules.Count - 1);
			}
		}

		pathModules.RemoveAt(pathModules.Count - 1);
	}

	private static string CreateCanonicalKey(ImmutableArray<string> modules)
	{
		var candidates = new string[modules.Length];
		for (var offset = 0; offset < modules.Length; offset++)
		{
			candidates[offset] = string.Join("|", modules.Skip(offset).Concat(modules.Take(offset)));
		}

		var result = candidates.OrderBy(candidate => candidate, StringComparer.Ordinal).First();

		return result;
	}
}
