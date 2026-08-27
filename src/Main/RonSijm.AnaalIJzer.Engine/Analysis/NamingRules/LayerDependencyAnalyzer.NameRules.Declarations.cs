using System.Collections.Concurrent;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using RonSijm.AnaalIJzer.Core.Indicators;
using RonSijm.AnaalIJzer.Core.NameRules;
using RonSijm.AnaalIJzer.Core.Violations;
using AnalyzerConfig = RonSijm.AnaalIJzer.Core.RuntimeConfig.Config.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer.Engine.Analysis.NamingRules;

public static partial class LayerDependencyAnalyzer
{
    public static void AnalyzeParameterDeclarationNameRules(SyntaxNodeAnalysisContext context, AnalyzerConfig config, ConcurrentBag<ViolationRecord> violations, SeparatedSyntaxList<ParameterSyntax> parameters, string site)
	{
		foreach (var parameter in parameters)
		{
			if (context.SemanticModel.GetDeclaredSymbol(parameter, context.CancellationToken) is not IParameterSymbol parameterSymbol)
			{
				continue;
			}

			AnalyzeDeclarationNameRule(context, config, violations, parameterSymbol.Type, parameter.Identifier.ValueText, site, parameter.Identifier.GetLocation());
		}
	}

    public static void AnalyzeMethodReturnDeclarationNameRule(SyntaxNodeAnalysisContext context, AnalyzerConfig config, ConcurrentBag<ViolationRecord> violations, MethodDeclarationSyntax method)
	{
		if (context.SemanticModel.GetDeclaredSymbol(method, context.CancellationToken) is not IMethodSymbol methodSymbol)
		{
			return;
		}

		AnalyzeDeclarationNameRule(context, config, violations, methodSymbol.ReturnType, method.Identifier.ValueText, DependencySites.MethodReturn, method.Identifier.GetLocation());
	}

    public static void AnalyzeFieldDeclarationNameRules(SyntaxNodeAnalysisContext context, AnalyzerConfig config, ConcurrentBag<ViolationRecord> violations, FieldDeclarationSyntax field)
	{
		foreach (var variable in field.Declaration.Variables)
		{
			if (context.SemanticModel.GetDeclaredSymbol(variable, context.CancellationToken) is not IFieldSymbol fieldSymbol)
			{
				continue;
			}

			AnalyzeDeclarationNameRule(context, config, violations, fieldSymbol.Type, variable.Identifier.ValueText, DependencySites.Field, variable.Identifier.GetLocation());
		}
	}

    public static void AnalyzePropertyDeclarationNameRule(SyntaxNodeAnalysisContext context, AnalyzerConfig config, ConcurrentBag<ViolationRecord> violations, PropertyDeclarationSyntax property)
	{
		if (context.SemanticModel.GetDeclaredSymbol(property, context.CancellationToken) is not IPropertySymbol propertySymbol)
		{
			return;
		}

		AnalyzeDeclarationNameRule(context, config, violations, propertySymbol.Type, property.Identifier.ValueText, DependencySites.Property, property.Identifier.GetLocation());
	}

    public static void AnalyzeLocalDeclarationNameRules(SyntaxNodeAnalysisContext context, AnalyzerConfig config, ConcurrentBag<ViolationRecord> violations, LocalDeclarationStatementSyntax local)
	{
		foreach (var variable in local.Declaration.Variables)
		{
			if (context.SemanticModel.GetDeclaredSymbol(variable, context.CancellationToken) is not ILocalSymbol localSymbol)
			{
				continue;
			}

			AnalyzeDeclarationNameRule(context, config, violations, localSymbol.Type, variable.Identifier.ValueText, DependencySites.Local, variable.Identifier.GetLocation());
		}
	}

	private static void AnalyzeDeclarationNameRule(SyntaxNodeAnalysisContext context, AnalyzerConfig config, ConcurrentBag<ViolationRecord> violations, ITypeSymbol type, string declaredName, string site, Location reportLocation)
	{
		var caller = BoundaryRules.LayerDependencies.LayerDependencyAnalyzer.TryGetCallerLayer(context, config, context.Node);
		if (caller is null)
		{
			return;
		}

		var source = NameRuleSubjectFactory.CreateType(type);
		if (source is null)
		{
			return;
		}

		var target = NameRuleSubjectFactory.CreateDeclarationName(declaredName, type);
		var violation = config.Engine.EvaluateNameRules(caller.Value.Match, NameRuleTrigger.Declaration, source.Value, target, site);
		if (violation is null)
		{
			return;
		}

		ReportNameRuleViolation(context, violations, caller.Value.TypeName, caller.Value.Match.Layer.Name, violation.Value, reportLocation);
	}
}
