using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RonSijm.AnaalIJzer.Indicators;
using RonSijm.AnaalIJzer.Engine.NameRules;
using ProjectAnalyzerConfig = RonSijm.AnaalIJzer.Model.AnalyzerConfig;
using RonSijm.AnaalIJzer.Core.Editor.Snapshots;

namespace RonSijm.AnaalIJzer.EditorRuntime.Snapshots;

public static partial class ArchitectureEditorSnapshotService
{
	private static void AddNameRuleIndicators(SyntaxNode node, SemanticModel semanticModel, ProjectAnalyzerConfig config, ImmutableArray<ArchitectureNameRuleIndicator>.Builder indicators, CancellationToken cancellationToken)
	{
		switch (node)
		{
			case ConstructorDeclarationSyntax constructor:
				AddParameterNameRuleIndicators(constructor.ParameterList.Parameters, constructor, DependencySites.Constructor, semanticModel, config, indicators, cancellationToken);
				break;
			case TypeDeclarationSyntax typeDeclaration:
				AddPrimaryConstructorNameRuleIndicators(typeDeclaration, semanticModel, config, indicators, cancellationToken);
				break;
			case MethodDeclarationSyntax method:
				if (semanticModel.GetDeclaredSymbol(method, cancellationToken) is IMethodSymbol methodSymbol)
				{
					AddNameRuleIndicator(method, method.Identifier.Span, methodSymbol.ReturnType, method.Identifier.ValueText, DependencySites.MethodReturn, semanticModel, config, indicators, cancellationToken);
				}
				AddParameterNameRuleIndicators(method.ParameterList.Parameters, method, DependencySites.Method, semanticModel, config, indicators, cancellationToken);
				break;
			case FieldDeclarationSyntax field:
				foreach (var variable in field.Declaration.Variables)
				{
					if (semanticModel.GetDeclaredSymbol(variable, cancellationToken) is IFieldSymbol fieldSymbol)
					{
						AddNameRuleIndicator(field, variable.Identifier.Span, fieldSymbol.Type, variable.Identifier.ValueText, DependencySites.Field, semanticModel, config, indicators, cancellationToken);
					}
				}
				break;
			case PropertyDeclarationSyntax property when semanticModel.GetDeclaredSymbol(property, cancellationToken) is IPropertySymbol propertySymbol:
				AddNameRuleIndicator(property, property.Identifier.Span, propertySymbol.Type, property.Identifier.ValueText, DependencySites.Property, semanticModel, config, indicators, cancellationToken);
				break;
			case LocalDeclarationStatementSyntax local:
				foreach (var variable in local.Declaration.Variables)
				{
					if (semanticModel.GetDeclaredSymbol(variable, cancellationToken) is ILocalSymbol localSymbol)
					{
						AddNameRuleIndicator(local, variable.Identifier.Span, localSymbol.Type, variable.Identifier.ValueText, DependencySites.Local, semanticModel, config, indicators, cancellationToken);
					}
				}
				break;
		}
	}

	private static void AddPrimaryConstructorNameRuleIndicators(TypeDeclarationSyntax typeDeclaration, SemanticModel semanticModel, ProjectAnalyzerConfig config, ImmutableArray<ArchitectureNameRuleIndicator>.Builder indicators, CancellationToken cancellationToken)
	{
		var parameterList = typeDeclaration switch
		{
			ClassDeclarationSyntax classDeclaration => classDeclaration.ParameterList,
			StructDeclarationSyntax structDeclaration => structDeclaration.ParameterList,
			RecordDeclarationSyntax recordDeclaration => recordDeclaration.ParameterList,
			_ => null
		};
		if (parameterList is not null)
		{
			AddParameterNameRuleIndicators(parameterList.Parameters, typeDeclaration, DependencySites.Constructor, semanticModel, config, indicators, cancellationToken);
		}
	}

	private static void AddParameterNameRuleIndicators(SeparatedSyntaxList<ParameterSyntax> parameters, SyntaxNode callerNode, string site, SemanticModel semanticModel, ProjectAnalyzerConfig config, ImmutableArray<ArchitectureNameRuleIndicator>.Builder indicators, CancellationToken cancellationToken)
	{
		foreach (var parameter in parameters)
		{
			if (semanticModel.GetDeclaredSymbol(parameter, cancellationToken) is IParameterSymbol parameterSymbol)
			{
				AddNameRuleIndicator(callerNode, parameter.Identifier.Span, parameterSymbol.Type, parameter.Identifier.ValueText, site, semanticModel, config, indicators, cancellationToken);
			}
		}
	}

	private static void AddNameRuleIndicator(SyntaxNode callerNode, Microsoft.CodeAnalysis.Text.TextSpan span, ITypeSymbol type, string declaredName, string site, SemanticModel semanticModel, ProjectAnalyzerConfig config, ImmutableArray<ArchitectureNameRuleIndicator>.Builder indicators, CancellationToken cancellationToken)
	{
		if (TryGetCaller(callerNode, semanticModel, config, cancellationToken) is not { } caller)
		{
			return;
		}

		var source = NameRuleSubjectFactory.CreateType(type);
		if (source is null)
		{
			return;
		}

		var target = NameRuleSubjectFactory.CreateDeclarationName(declaredName, type);
		var violation = config.Engine.EvaluateNameRules(caller.Match, NameRuleTrigger.Declaration, source.Value, target, site);
		if (violation is null)
		{
			return;
		}

		indicators.Add(new ArchitectureNameRuleIndicator(span, site, violation.Value.RuleKind.ToString(), caller.TypeName, caller.LayerPath, violation.Value.SourceName, violation.Value.TargetName, violation.Value.NormalizedSourceName, violation.Value.NormalizedTargetName, violation.Value.Reason));
	}
}
