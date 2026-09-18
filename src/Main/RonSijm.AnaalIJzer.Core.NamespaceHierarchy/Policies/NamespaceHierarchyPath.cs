using System.Collections.Immutable;

namespace RonSijm.AnaalIJzer.Core.NamespaceHierarchy.Policies;

public readonly struct NamespaceHierarchyPath(ImmutableArray<string> segments)
{
	private readonly ImmutableArray<string> _segments = segments.IsDefault ? ImmutableArray<string>.Empty : segments;

	public ImmutableArray<string> Segments => _segments;

	public int Length => _segments.Length;

	public static bool TryParse(string? value, out NamespaceHierarchyPath path)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			path = default;

			return false;
		}

		var segments = value!.Split('.');
		if (segments.Any(segment => string.IsNullOrWhiteSpace(segment) || segment.Any(char.IsWhiteSpace)))
		{
			path = default;

			return false;
		}

		path = new NamespaceHierarchyPath([.. segments]);

		return true;
	}

	public bool IsAncestorOfOrEqualTo(NamespaceHierarchyPath candidate)
	{
		if (Length > candidate.Length)
		{
			return false;
		}

		for (var index = 0; index < Length; index++)
		{
			if (!string.Equals(_segments[index], candidate._segments[index], StringComparison.Ordinal))
			{
				return false;
			}
		}

		return true;
	}

	public bool Equals(NamespaceHierarchyPath other)
	{
		if (Length != other.Length)
		{
			return false;
		}

		return IsAncestorOfOrEqualTo(other);
	}

	public NamespaceHierarchyRelation GetRelationTo(NamespaceHierarchyPath dependency)
	{
		if (Equals(dependency))
		{
			return NamespaceHierarchyRelation.SameNamespace;
		}

		if (IsAncestorOfOrEqualTo(dependency))
		{
			return NamespaceHierarchyRelation.AncestorToDescendant;
		}

		if (dependency.IsAncestorOfOrEqualTo(this))
		{
			return NamespaceHierarchyRelation.DescendantToAncestor;
		}

		return NamespaceHierarchyRelation.SiblingToSibling;
	}

	public override string ToString()
	{
		var result = string.Join(".", _segments);

		return result;
	}
}
