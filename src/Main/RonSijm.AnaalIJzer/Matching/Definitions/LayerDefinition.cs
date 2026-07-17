namespace RonSijm.AnaalIJzer.Definitions;

/// <summary>Defines a named architectural layer.</summary>
internal readonly struct LayerDefinition
{
	private LayerDefinition(string name, bool isForbidden, string? comment, string? fixSuffix)
	{
		Name = name;
		IsForbidden = isForbidden;
		Comment = comment;
		FixSuffix = fixSuffix;
	}

	/// <summary>Display name used in <c>AllowedDependency</c> edges and diagnostic messages.</summary>
	public string Name { get; }

	/// <summary>When <see langword="true" /> depending on this layer always produces ARCH003.</summary>
	public bool IsForbidden { get; }

	public string? Comment { get; }

	/// <summary>
	///     When set, a Roslyn code fix will offer to rename types matched via <c>endsWith</c>
	///     by replacing the matched suffix with this value.
	/// </summary>
	public string? FixSuffix { get; }

	public static LayerDefinition Normal(string name, string? comment)
	{
		var result = new LayerDefinition(name, false, comment, null);

		return result;
	}

	public static LayerDefinition Forbidden(string name, string? comment, string? fixSuffix)
	{
		var result = new LayerDefinition(name, true, comment, fixSuffix);

		return result;
	}
}
