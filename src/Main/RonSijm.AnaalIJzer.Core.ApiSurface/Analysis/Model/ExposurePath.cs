using System.Collections.Immutable;

namespace RonSijm.AnaalIJzer.Core.ApiSurface.Analysis.Model;

public readonly struct ExposurePath(string rootMember, ImmutableArray<ExposurePathSegment> segments)
{
	public string RootMember { get; } = rootMember;
	public ImmutableArray<ExposurePathSegment> Segments { get; } = segments;

	public ExposurePath Append(ExposurePathSegment segment)
	{
		var result = new ExposurePath(RootMember, Segments.Add(segment));

		return result;
	}

	public string ToDisplayText(string finalTypeName)
	{
		var parts = new List<string> { RootMember };
		parts.AddRange(Segments.Select(segment => segment.DisplayName));
		parts.Add(finalTypeName);
		var result = string.Join(" -> ", parts);

		return result;
	}
}
