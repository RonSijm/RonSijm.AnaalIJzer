using System.Collections.Immutable;

namespace RonSijm.AnaalIJzer.Indicators;

public static class ArchitectureDependencySites
{
	public const string Constructor = DependencySites.Constructor;
	public const string Method = DependencySites.Method;
	public const string MethodReturn = DependencySites.MethodReturn;
	public const string Field = DependencySites.Field;
	public const string Property = DependencySites.Property;
	public const string Local = DependencySites.Local;
	public const string New = DependencySites.New;
	public const string GenericInvocation = DependencySites.GenericInvocation;
	public const string GenericArgument = DependencySites.GenericArgument;
	public const string Inheritance = DependencySites.Inheritance;
	public const string InterfaceImplementation = DependencySites.InterfaceImplementation;
	public const string Attribute = DependencySites.Attribute;
	public const string StaticMember = DependencySites.StaticMember;

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
		StaticMember,
	];
}
