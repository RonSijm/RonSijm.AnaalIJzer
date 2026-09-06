namespace RonSijm.AnaalIJzer.Core.NameRules;

/// <summary>Controls how far a value-name rule follows a local value before comparing names.</summary>
public enum NameRuleValueTrackingMode
{
	/// <summary>Compare the immediate source and target syntax only.</summary>
	Direct,

	/// <summary>Also follow unambiguous local aliases inside one bounded method-like or lambda body.</summary>
	IntraProcedural,
}
