namespace RonSijm.AnaalIJzer.Core.NameRules;

public static class NameRuleValueTrackingModeParser
{
	public static bool TryParse(string? value, out NameRuleValueTrackingMode mode)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			mode = NameRuleValueTrackingMode.Direct;
			var defaultResult = true;

			return defaultResult;
		}

		var parsed = value!.Trim().ToLowerInvariant() switch
		{
			"direct" => NameRuleValueTrackingMode.Direct,
			"intraprocedural" => NameRuleValueTrackingMode.IntraProcedural,
			_ => (NameRuleValueTrackingMode?)null,
		};
		if (parsed is null)
		{
			mode = default;
			var invalidResult = false;

			return invalidResult;
		}

		mode = parsed.Value;
		var result = true;

		return result;
	}
}
