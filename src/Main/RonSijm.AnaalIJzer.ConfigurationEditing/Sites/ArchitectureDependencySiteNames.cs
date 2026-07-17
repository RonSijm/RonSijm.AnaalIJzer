using System.Collections.Immutable;

namespace RonSijm.AnaalIJzer.ConfigurationEditing.Sites;

public static class ArchitectureDependencySiteNames
{
	public const string Constructor = "Constructor";
	public const string Method = "Method";
	public const string MethodReturn = "MethodReturn";
	public const string Field = "Field";
	public const string Property = "Property";
	public const string Local = "Local";
	public const string New = "New";
	public const string GenericInvocation = "GenericInvocation";
	public const string GenericArgument = "GenericArgument";
	public const string Inheritance = "Inheritance";
	public const string InterfaceImplementation = "InterfaceImplementation";
	public const string Attribute = "Attribute";
	public const string StaticMember = "StaticMember";

	public static ImmutableArray<string> All { get; } =
	[
		Constructor,
		Method,
		MethodReturn,
		Field,
		Property,
		Local,
		New,
		GenericInvocation,
		GenericArgument,
		Inheritance,
		InterfaceImplementation,
		Attribute,
		StaticMember
	];
}
