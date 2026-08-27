namespace RonSijm.AnaalIJzer.Core.Exceptions;

public static class ArchitectureExceptionEvaluator
{
	public static ArchitectureExceptionReview? Evaluate(
		ArchitectureExceptionPolicy policy,
		string matcherKind,
		string matcherLabel,
		ArchitectureExceptionMetadata metadata,
		string ownerLayerPath,
		string xmlPath,
		int xmlLineNumber,
		int xmlLinePosition)
	{
		if (!policy.IsEnabled)
		{
			return null;
		}

		if (policy.RequireReason && string.IsNullOrWhiteSpace(metadata.Reason))
		{
			return CreateReview(matcherKind, matcherLabel, metadata, ArchitectureExceptionStatus.Invalid, $"Architecture exception for {matcherKind} '{matcherLabel}' is missing required reason metadata", ownerLayerPath, xmlPath, xmlLineNumber, xmlLinePosition);
		}

		if (policy.RequireOwner && string.IsNullOrWhiteSpace(metadata.Owner))
		{
			return CreateReview(matcherKind, matcherLabel, metadata, ArchitectureExceptionStatus.Invalid, $"Architecture exception for {matcherKind} '{matcherLabel}' is missing required owner metadata", ownerLayerPath, xmlPath, xmlLineNumber, xmlLinePosition);
		}

		if (policy.RequireExpiresOn && string.IsNullOrWhiteSpace(metadata.ExpiresOnText))
		{
			return CreateReview(matcherKind, matcherLabel, metadata, ArchitectureExceptionStatus.Invalid, $"Architecture exception for {matcherKind} '{matcherLabel}' is missing required expiresOn metadata", ownerLayerPath, xmlPath, xmlLineNumber, xmlLinePosition);
		}

		if (!string.IsNullOrWhiteSpace(metadata.ExpiresOnText) && metadata.ExpiresOn is null)
		{
			return CreateReview(matcherKind, matcherLabel, metadata, ArchitectureExceptionStatus.Invalid, $"Architecture exception for {matcherKind} '{matcherLabel}' has an invalid expiresOn date '{metadata.ExpiresOnText}'", ownerLayerPath, xmlPath, xmlLineNumber, xmlLinePosition);
		}

		if (metadata.ExpiresOn is not { } expiresOn)
		{
			return null;
		}

		var today = ArchitectureClock.UtcToday;
		if (expiresOn < today)
		{
			return CreateReview(matcherKind, matcherLabel, metadata, ArchitectureExceptionStatus.Expired, $"Architecture exception for {matcherKind} '{matcherLabel}' has expired on {expiresOn:yyyy-MM-dd} and is no longer applied", ownerLayerPath, xmlPath, xmlLineNumber, xmlLinePosition);
		}

		var daysUntilExpiry = (expiresOn - today).Days;
		if (daysUntilExpiry <= policy.WarnBeforeDays)
		{
			return CreateReview(matcherKind, matcherLabel, metadata, ArchitectureExceptionStatus.ExpiringSoon, $"Architecture exception for {matcherKind} '{matcherLabel}' expires in {daysUntilExpiry} day{(daysUntilExpiry == 1 ? string.Empty : "s")} on {expiresOn:yyyy-MM-dd}", ownerLayerPath, xmlPath, xmlLineNumber, xmlLinePosition);
		}

		return null;
	}

	public static string CreateStaleMessage(ArchitectureExceptionDefinition definition, string scopeLabel)
	{
		var result = $"Architecture exception for {definition.MatcherKind} '{definition.MatcherLabel}' is stale: it matches no type in the inspected {scopeLabel}";

		return result;
	}

	private static ArchitectureExceptionReview CreateReview(
		string matcherKind,
		string matcherLabel,
		ArchitectureExceptionMetadata metadata,
		ArchitectureExceptionStatus status,
		string message,
		string ownerLayerPath,
		string xmlPath,
		int xmlLineNumber,
		int xmlLinePosition)
	{
		var result = new ArchitectureExceptionReview(matcherKind, matcherLabel, metadata, status, message, ownerLayerPath, xmlPath, xmlLineNumber, xmlLinePosition);

		return result;
	}
}
