namespace RonSijm.AnaalIJzer.SymbolFacts;

public enum ArchitectureAccessibility
{
	Public,
	Internal,
	Protected,
	ProtectedInternal,
	PrivateProtected,
	Private,
	File
}

public static class ArchitectureAccessibilityParser
{
	public static bool TryParse(string value, out ArchitectureAccessibility accessibility)
	{
		var result = Enum.TryParse(value.Trim(), true, out accessibility)
		             && Enum.IsDefined(typeof(ArchitectureAccessibility), accessibility);

		return result;
	}

	public static string ToDisplayText(this ArchitectureAccessibility accessibility)
	{
		var result = accessibility switch
		{
			ArchitectureAccessibility.ProtectedInternal => "ProtectedInternal",
			ArchitectureAccessibility.PrivateProtected => "PrivateProtected",
			_ => accessibility.ToString()
		};

		return result;
	}
}
