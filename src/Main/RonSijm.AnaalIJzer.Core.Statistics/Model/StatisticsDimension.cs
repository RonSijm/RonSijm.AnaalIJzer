namespace RonSijm.AnaalIJzer.Core.Statistics.Model;

public enum StatisticsDimension
{
	TypeKind,
	DependencySite,
	TypeAccessibility,
	MemberAccessibility,
	MemberKind
}

public static class StatisticsDimensionCatalog
{
	public const string Other = "Other";
	public const string NotApplicable = "NotApplicable";

	public static readonly string[] TypeKinds = ["Class", "Interface", "Struct", "Record", "RecordStruct", "Enum", "Delegate", Other];
	public static readonly string[] DependencySites = ["Constructor", "Method", "MethodReturn", "Field", "Property", "Local", "New", "GenericInvocation", "GenericArgument", "Inheritance", "InterfaceImplementation", "Attribute", "StaticMember", Other];
	public static readonly string[] Accessibilities = ["Public", "Internal", "Protected", "ProtectedInternal", "PrivateProtected", "Private", "File", NotApplicable];
	public static readonly string[] MemberKinds = ["Constructor", "Method", "Property", "Field", "Event", Other];

	public static IReadOnlyList<string> GetCanonicalBuckets(StatisticsDimension dimension)
	{
		var result = dimension switch
		{
			StatisticsDimension.TypeKind => TypeKinds,
			StatisticsDimension.DependencySite => DependencySites,
			StatisticsDimension.TypeAccessibility => Accessibilities,
			StatisticsDimension.MemberAccessibility => Accessibilities,
			StatisticsDimension.MemberKind => MemberKinds,
			_ => []
		};

		return result;
	}

	public static int GetBucketOrder(StatisticsDimension dimension, string bucket)
	{
		var buckets = GetCanonicalBuckets(dimension);
		var result = int.MaxValue;
		for (var index = 0; index < buckets.Count; index++)
		{
			if (string.Equals(buckets[index], bucket, StringComparison.Ordinal))
			{
				result = index;
				break;
			}
		}

		return result;
	}

	public static IReadOnlyList<StatisticsDimension> GetDefaultGroupDimensions(StatisticsDimension dimension)
	{
		var result = dimension switch
		{
			StatisticsDimension.TypeKind => new[] { StatisticsDimension.TypeAccessibility },
			StatisticsDimension.TypeAccessibility => new[] { StatisticsDimension.TypeKind },
			StatisticsDimension.DependencySite => new[] { StatisticsDimension.TypeKind, StatisticsDimension.TypeAccessibility },
			StatisticsDimension.MemberAccessibility => new[] { StatisticsDimension.MemberKind },
			StatisticsDimension.MemberKind => new[] { StatisticsDimension.MemberAccessibility },
			_ => []
		};

		return result;
	}
}
