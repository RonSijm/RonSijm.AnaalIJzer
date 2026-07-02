using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using RonSijm.AnaalIJzer.Config;
using RonSijm.AnaalIJzer.Diagnostics;
using RonSijm.AnaalIJzer.Matching;
using RonSijm.AnaalIJzer.Reporting;
using AnalyzerConfig = RonSijm.AnaalIJzer.Config.AnalyzerConfig;

namespace RonSijm.AnaalIJzer.Analysis;

/// <summary>
///     Analyses dependency usages including signatures, state, locals, inheritance, attributes,
///     static access, object creation and generic invocations to enforce architectural layer rules.
/// </summary>
internal static class LayerDependencyAnalyzer
{
	internal static void AnalyzeConstructorDeclaration(SyntaxNodeAnalysisContext context, AnalyzerConfig config, ConcurrentBag<ViolationRecord> violations)
	{
		var ctorDecl = (ConstructorDeclarationSyntax)context.Node;

		if (ctorDecl.Parent is not TypeDeclarationSyntax typeDeclaration)
		{
			return;
		}

		AnalyzeParameters(context, config, violations, typeDeclaration.Identifier.ValueText, GetContainingNamespace(typeDeclaration), ctorDecl.ParameterList.Parameters, DependencySites.Constructor, applyStrict: typeDeclaration is ClassDeclarationSyntax);
	}

	internal static void AnalyzeTypeDeclaration(SyntaxNodeAnalysisContext context, AnalyzerConfig config, ConcurrentBag<ViolationRecord> violations)
	{
		var typeDeclaration = (TypeDeclarationSyntax)context.Node;
		var parameterList = typeDeclaration switch
		{
			ClassDeclarationSyntax classDeclaration => classDeclaration.ParameterList,
			StructDeclarationSyntax structDeclaration => structDeclaration.ParameterList,
			RecordDeclarationSyntax recordDeclaration => recordDeclaration.ParameterList,
			_ => null
		};

		if (parameterList is not null && parameterList.Parameters.Count > 0)
		{
			AnalyzeParameters(context, config, violations, typeDeclaration.Identifier.ValueText, GetContainingNamespace(typeDeclaration), parameterList.Parameters, DependencySites.Constructor, applyStrict: typeDeclaration is ClassDeclarationSyntax);
		}

		if (typeDeclaration.BaseList is null)
		{
			return;
		}

		var caller = TryGetCallerLayer(context, config, typeDeclaration);
		if (caller is null)
		{
			return;
		}

		foreach (var baseType in typeDeclaration.BaseList.Types)
		{
			var type = context.SemanticModel.GetTypeInfo(baseType.Type, context.CancellationToken).Type;
			if (type is not null)
			{
				AnalyzeTypeReference(context, config, violations, caller.Value.TypeName, caller.Value.Match, baseType.Type.GetLocation(), type, applyStrict: false, DependencySites.Inheritance);
			}
		}
	}

	internal static void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context, AnalyzerConfig config, ConcurrentBag<ViolationRecord> violations)
	{
		var methodDecl = (MethodDeclarationSyntax)context.Node;

		if (methodDecl.Parent is not TypeDeclarationSyntax typeDeclaration)
		{
			return;
		}

		var caller = TryGetCallerLayer(context, config, methodDecl);
		if (caller is null)
		{
			return;
		}

		var returnTypeInfo = context.SemanticModel.GetTypeInfo(methodDecl.ReturnType, context.CancellationToken);
		if (returnTypeInfo.Type is not null)
		{
			AnalyzeTypeReference(context, config, violations, caller.Value.TypeName, caller.Value.Match, methodDecl.ReturnType.GetLocation(), returnTypeInfo.Type, applyStrict: false, DependencySites.MethodReturn);
		}

		if (methodDecl.ParameterList.Parameters.Count > 0)
		{
			AnalyzeParameters(context, config, violations, typeDeclaration.Identifier.ValueText, GetContainingNamespace(typeDeclaration), methodDecl.ParameterList.Parameters, DependencySites.Method, applyStrict: false);
		}
	}

	internal static void AnalyzeFieldDeclaration(SyntaxNodeAnalysisContext context, AnalyzerConfig config, ConcurrentBag<ViolationRecord> violations)
	{
		var fieldDecl = (FieldDeclarationSyntax)context.Node;
		var caller = TryGetCallerLayer(context, config, fieldDecl);
		if (caller is null)
		{
			return;
		}

		var typeSyntax = fieldDecl.Declaration.Type;
		var typeInfo = context.SemanticModel.GetTypeInfo(typeSyntax, context.CancellationToken);
		if (typeInfo.Type is null)
		{
			return;
		}

		AnalyzeTypeReference(context, config, violations, caller.Value.TypeName, caller.Value.Match, typeSyntax.GetLocation(), typeInfo.Type, applyStrict: false, DependencySites.Field);
	}

	internal static void AnalyzePropertyDeclaration(SyntaxNodeAnalysisContext context, AnalyzerConfig config, ConcurrentBag<ViolationRecord> violations)
	{
		var propDecl = (PropertyDeclarationSyntax)context.Node;
		var caller = TryGetCallerLayer(context, config, propDecl);
		if (caller is null)
		{
			return;
		}

		var typeInfo = context.SemanticModel.GetTypeInfo(propDecl.Type, context.CancellationToken);
		if (typeInfo.Type is null)
		{
			return;
		}

		AnalyzeTypeReference(context, config, violations, caller.Value.TypeName, caller.Value.Match, propDecl.Type.GetLocation(), typeInfo.Type, applyStrict: false, DependencySites.Property);
	}

	internal static void AnalyzeLocalDeclaration(SyntaxNodeAnalysisContext context, AnalyzerConfig config, ConcurrentBag<ViolationRecord> violations)
	{
		var localDecl = (LocalDeclarationStatementSyntax)context.Node;
		var caller = TryGetCallerLayer(context, config, localDecl);
		if (caller is null)
		{
			return;
		}

		var typeSyntax = localDecl.Declaration.Type;
		var typeInfo = context.SemanticModel.GetTypeInfo(typeSyntax, context.CancellationToken);
		if (typeInfo.Type is not null && typeInfo.Type.TypeKind != TypeKind.Error)
		{
			AnalyzeTypeReference(context, config, violations, caller.Value.TypeName, caller.Value.Match, typeSyntax.GetLocation(), typeInfo.Type, applyStrict: false, DependencySites.Local);
			return;
		}

		foreach (var variable in localDecl.Declaration.Variables)
		{
			if (context.SemanticModel.GetDeclaredSymbol(variable, context.CancellationToken) is not ILocalSymbol localSymbol || localSymbol.Type.TypeKind == TypeKind.Error)
			{
				continue;
			}

			AnalyzeTypeReference(context, config, violations, caller.Value.TypeName, caller.Value.Match, variable.Identifier.GetLocation(), localSymbol.Type, applyStrict: false, DependencySites.Local);
		}
	}

	internal static void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context, AnalyzerConfig config, ConcurrentBag<ViolationRecord> violations)
	{
		var node = (ExpressionSyntax)context.Node;
		var caller = TryGetCallerLayer(context, config, node);
		if (caller is null)
		{
			return;
		}

		var typeInfo = context.SemanticModel.GetTypeInfo(node, context.CancellationToken);
		if (typeInfo.Type is null)
		{
			return;
		}

		// Squiggle the type name when it's explicit; otherwise the whole 'new(...)' expression.
		var location = node is ObjectCreationExpressionSyntax oce
			? oce.Type.GetLocation()
			: node.GetLocation();

		AnalyzeTypeReference(context, config, violations, caller.Value.TypeName, caller.Value.Match, location, typeInfo.Type, applyStrict: false, DependencySites.New);
	}

	internal static void AnalyzeInvocation(SyntaxNodeAnalysisContext context, AnalyzerConfig config, ConcurrentBag<ViolationRecord> violations)
	{
		var invocation = (InvocationExpressionSyntax)context.Node;
		var caller = TryGetCallerLayer(context, config, invocation);
		if (caller is null)
		{
			return;
		}

		if (context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol is IMethodSymbol method)
		{
			var staticContainer = method.IsStatic ? method.ContainingType : method.ReducedFrom?.ContainingType;
			if (staticContainer is not null)
			{
				var staticLocation = invocation.Expression is MemberAccessExpressionSyntax memberAccess
					? memberAccess.Expression.GetLocation()
					: invocation.Expression.GetLocation();
				AnalyzeTypeReference(context, config, violations, caller.Value.TypeName, caller.Value.Match, staticLocation, staticContainer, applyStrict: false, DependencySites.StaticMember);
			}
		}

		// Service-locator style: only generic method invocations carry a type-argument
		// dependency that the caller has not declared elsewhere.
		var generic = invocation.Expression switch
		{
			MemberAccessExpressionSyntax m => m.Name as GenericNameSyntax,
			GenericNameSyntax g => g,
			_ => null,
		};

		if (generic is null || generic.TypeArgumentList.Arguments.Count == 0)
		{
			return;
		}

		foreach (var typeArg in generic.TypeArgumentList.Arguments)
		{
			var typeInfo = context.SemanticModel.GetTypeInfo(typeArg, context.CancellationToken);
			if (typeInfo.Type is null)
			{
				continue;
			}

			AnalyzeTypeReference(context, config, violations, caller.Value.TypeName, caller.Value.Match, typeArg.GetLocation(), typeInfo.Type, applyStrict: false, DependencySites.GenericInvocation);
		}
	}

	internal static void AnalyzeAttribute(SyntaxNodeAnalysisContext context, AnalyzerConfig config, ConcurrentBag<ViolationRecord> violations)
	{
		var attribute = (AttributeSyntax)context.Node;
		var caller = TryGetCallerLayer(context, config, attribute);
		if (caller is null)
		{
			return;
		}

		if (context.SemanticModel.GetSymbolInfo(attribute, context.CancellationToken).Symbol is not IMethodSymbol constructor)
		{
			return;
		}

		AnalyzeTypeReference(context, config, violations, caller.Value.TypeName, caller.Value.Match, attribute.Name.GetLocation(), constructor.ContainingType, applyStrict: false, DependencySites.Attribute);
	}

	internal static void AnalyzeStaticMemberAccess(SyntaxNodeAnalysisContext context, AnalyzerConfig config, ConcurrentBag<ViolationRecord> violations)
	{
		var memberAccess = (MemberAccessExpressionSyntax)context.Node;
		var symbol = context.SemanticModel.GetSymbolInfo(memberAccess, context.CancellationToken).Symbol;
		var containingType = symbol switch
		{
			IPropertySymbol { IsStatic: true } property => property.ContainingType,
			IFieldSymbol { IsStatic: true } field => field.ContainingType,
			IEventSymbol { IsStatic: true } @event => @event.ContainingType,
			_ => null
		};
		if (containingType is null)
		{
			return;
		}

		var caller = TryGetCallerLayer(context, config, memberAccess);
		if (caller is null)
		{
			return;
		}

		AnalyzeTypeReference(context, config, violations, caller.Value.TypeName, caller.Value.Match, memberAccess.Expression.GetLocation(), containingType, applyStrict: false, DependencySites.StaticMember);
	}

	private static void AnalyzeParameters(SyntaxNodeAnalysisContext context, AnalyzerConfig config, ConcurrentBag<ViolationRecord> violations, string callerTypeName, string callerNamespace, SeparatedSyntaxList<ParameterSyntax> parameters, string site, bool applyStrict)
	{
		var typeDeclaration = parameters.Count > 0 ? parameters[0].FirstAncestorOrSelf<TypeDeclarationSyntax>() : null;
		var callerSymbol = typeDeclaration is null ? null : context.SemanticModel.GetDeclaredSymbol(typeDeclaration, context.CancellationToken) as ITypeSymbol;

		var callerMatch = config.FindLayer(callerTypeName, callerNamespace, callerSymbol);
		if (callerMatch is null)
		{
			return;
		}

		// Forbidden types are never considered callers.
		if (callerMatch.Value.Layer.IsForbidden)
		{
			return;
		}

		foreach (var param in parameters)
		{
			var paramSymbol = context.SemanticModel.GetDeclaredSymbol(param, context.CancellationToken);
			if (paramSymbol is null)
			{
				continue;
			}

			AnalyzeTypeReference(context, config, violations, callerTypeName, callerMatch.Value, param.GetLocation(), paramSymbol.Type, applyStrict, site);
		}
	}

	/// <summary>
	///     Resolves the enclosing class declaration of <paramref name="node" /> to a layer.
	///     Returns <c>null</c> when the enclosing type is not configured or is a forbidden type
	///     (forbidden types never act as callers). Threads the class symbol through so semantic
	///     matchers (<c>inherits</c>, <c>implements</c>, <c>withAttribute</c>,
	///     <c>withAccessModifier</c>) can classify the caller.
	/// </summary>
	private static (string TypeName, LayerMatch Match)? TryGetCallerLayer(SyntaxNodeAnalysisContext context, AnalyzerConfig config, SyntaxNode node)
	{
		var typeDeclaration = node.FirstAncestorOrSelf<TypeDeclarationSyntax>();
		if (typeDeclaration is null)
		{
			return null;
		}

		var callerName = typeDeclaration.Identifier.ValueText;
		var callerNs = GetContainingNamespace(typeDeclaration);
		var callerSymbol = context.SemanticModel.GetDeclaredSymbol(typeDeclaration, context.CancellationToken) as ITypeSymbol;
		var match = config.FindLayer(callerName, callerNs, callerSymbol);
		if (match is null || match.Value.Layer.IsForbidden)
		{
			return null;
		}

		return (callerName, match.Value);
	}

	/// <summary>
	///     Evaluates the rules for a single dependency type, considering the outer type and
	///     every generic type argument (recursively). Each distinct type name produces at most
	///     one diagnostic at <paramref name="reportLocation" />. When <paramref name="applyStrict" />
	///     is false the ARCH002 strict-mode fallback is skipped — used for non-constructor and
	///     non-method-parameter sites where unleveled types would otherwise drown the report with
	///     primitives and framework types.
	/// </summary>
	private static void AnalyzeTypeReference(SyntaxNodeAnalysisContext context, AnalyzerConfig config, ConcurrentBag<ViolationRecord> violations, string callerTypeName, LayerMatch callerMatch, Location reportLocation, ITypeSymbol depType, bool applyStrict, string site)
	{
		var callerLayer = callerMatch.Layer;
		var seenDepTypeNames = new HashSet<string>(StringComparer.Ordinal);
		var matchedAnyLayer = false;
		var index = 0;

		foreach (var current in EnumerateTypeAndGenericArguments(depType))
		{
			var isOuter = index++ == 0;
			var effectiveSite = isOuter ? site : DependencySites.GenericArgument;

			var depTypeName = current.Name;
			if (string.IsNullOrEmpty(depTypeName))
			{
				continue;
			}

			// A type is always allowed to reference itself (e.g. ILogger<MyManager> inside MyManager).
			if (depTypeName == callerTypeName)
			{
				continue;
			}

			var depNamespace = current.ContainingNamespace?.ToString() ?? string.Empty;
			var depMatch = config.FindLayer(depTypeName, depNamespace, current);

			if (depMatch is null)
			{
				continue;
			}

			matchedAnyLayer = true;

			if (!seenDepTypeNames.Add(depTypeName))
			{
				continue;
			}

			var (depLayer, matchedSuffix) = (depMatch.Value.Layer, depMatch.Value.MatchedSuffix);
			var ruleProperties = BuildRuleProperties(depMatch.Value, depTypeName);

			if (depLayer.IsForbidden)
			{
				var reason = "the type matches a global <Forbidden> rule";
				if (depLayer.Comment is not null)
				{
					reason += $": {depLayer.Comment}";
				}

				// Include rename fix metadata when the matched suffix has a <Fix Rename="..."> defined.
				// Only attach for the outer type — the code-fix provider renames the parameter's
				// declared type, so a suffix taken from a nested generic argument would mis-target.
				var properties = AddViolationProperties(
					ruleProperties.Add(ArchitecturalDiagnostics.PropertySite, effectiveSite),
					callerTypeName,
					callerLayer.Name,
					depTypeName,
					depLayer.Name,
					reason,
					depLayer.Comment);
				if (isOuter && matchedSuffix is not null && depLayer.FixSuffix is not null)
				{
					properties = properties
						.Add(ArchitecturalDiagnostics.PropertyMatchedSuffix, matchedSuffix)
						.Add(ArchitecturalDiagnostics.PropertyFixSuffix, depLayer.FixSuffix);
				}

				context.ReportDiagnostic(Diagnostic.Create(
					ArchitecturalDiagnostics.ForbiddenDependency,
					reportLocation,
					properties,
					callerTypeName, callerLayer.Name, depTypeName, reason));

				violations.Add(new ViolationRecord(ArchitecturalDiagnosticIds.ForbiddenDependency, callerTypeName, callerLayer.Name, depTypeName, depLayer.Name, reason, depLayer.Comment));

				continue;
			}

			var typePolicyViolation = config.EvaluateTypePolicy(depMatch.Value, depTypeName, depNamespace, current);
			if (typePolicyViolation is { } policyViolation)
			{
				var policyRuleProperties = policyViolation.Rule is { } policyRule
					? BuildRuleProperties(policyRule, depTypeName)
					: ImmutableDictionary<string, string?>.Empty.Add(ArchitecturalDiagnostics.PropertyDepTypeName, depTypeName);
				var properties = AddViolationProperties(
					policyRuleProperties.Add(ArchitecturalDiagnostics.PropertySite, effectiveSite),
					callerTypeName,
					callerLayer.Name,
					depTypeName,
					policyViolation.DependencyLayerName,
					policyViolation.Reason,
					policyViolation.Comment);

				if (isOuter && policyViolation.Rule is { } matchedRule && policyViolation.MatchedSuffix is not null && matchedRule.Layer.FixSuffix is not null)
				{
					properties = properties
						.Add(ArchitecturalDiagnostics.PropertyMatchedSuffix, policyViolation.MatchedSuffix)
						.Add(ArchitecturalDiagnostics.PropertyFixSuffix, matchedRule.Layer.FixSuffix);
				}

				context.ReportDiagnostic(Diagnostic.Create(
					ArchitecturalDiagnostics.ForbiddenDependency,
					reportLocation,
					properties,
					callerTypeName, callerLayer.Name, depTypeName, policyViolation.Reason));

				violations.Add(new ViolationRecord(ArchitecturalDiagnosticIds.ForbiddenDependency, callerTypeName, callerLayer.Name, depTypeName, policyViolation.DependencyLayerName, policyViolation.Reason, policyViolation.Comment));
				continue;
			}

			var edgeEvaluation = config.Graph.EvaluateDependency(callerMatch, depMatch.Value, effectiveSite);
			if (edgeEvaluation.IsAllowed)
			{
				continue;
			}

			ReportIllegalDependency(context, violations, callerTypeName, callerLayer.Name, depTypeName, depLayer.Name, reportLocation, effectiveSite, config, ruleProperties, edgeEvaluation);
		}

		if (matchedAnyLayer || !applyStrict || !config.Strict)
		{
			return;
		}

		// Strict mode: neither the outer type nor any of its generic arguments is
		// assigned to a configured layer — report ARCH002 on the outer type.
		var outerName = depType.Name;
		var strictProperties = AddViolationProperties(
			ImmutableDictionary<string, string?>.Empty.Add(ArchitecturalDiagnostics.PropertySite, site),
			callerTypeName,
			callerLayer.Name,
			outerName,
			string.Empty,
			string.Empty,
			null);

		context.ReportDiagnostic(Diagnostic.Create(
			ArchitecturalDiagnostics.UnrecognizedDependency,
			reportLocation,
			strictProperties,
			callerTypeName, callerLayer.Name, outerName, string.Empty));

		violations.Add(new ViolationRecord(ArchitecturalDiagnosticIds.UnrecognizedDependency, callerTypeName, callerLayer.Name, outerName, string.Empty, string.Empty, null));
	}

	/// <summary>
	///     Picks ARCH001 / ARCH004 / ARCH005 based on the semantic reason the dependency is
	///     illegal and reports the diagnostic with a <c>Site</c> property attached.
	/// </summary>
	private static void ReportIllegalDependency(SyntaxNodeAnalysisContext context, ConcurrentBag<ViolationRecord> violations, string callerTypeName, string callerLayerName, string depTypeName, string depLayerName, Location reportLocation, string site, AnalyzerConfig config, ImmutableDictionary<string, string?> ruleProperties, DependencyEdgeEvaluation edgeEvaluation)
	{
		DiagnosticDescriptor descriptor;
		string diagnosticId;
		string reason;

		if (callerLayerName == depLayerName)
		{
			descriptor = ArchitecturalDiagnostics.SameLayerDependency;
			diagnosticId = ArchitecturalDiagnosticIds.SameLayerDependency;
			reason = edgeEvaluation.IsDeniedBySiteFilter ? edgeEvaluation.DenialReason : $"types in the same layer ('{callerLayerName}') may not depend on each other";
		}
		else if (edgeEvaluation.IsDeniedByBlockedEdge)
		{
			descriptor = ArchitecturalDiagnostics.IllegalDependency;
			diagnosticId = ArchitecturalDiagnosticIds.IllegalLevelDependency;
			reason = edgeEvaluation.DenialReason;
		}
		else if (config.Graph.HasEdge(edgeEvaluation.ScopePath, depLayerName, callerLayerName))
		{
			descriptor = ArchitecturalDiagnostics.WrongDirectionDependency;
			diagnosticId = ArchitecturalDiagnosticIds.WrongDirectionDependency;
			reason = edgeEvaluation.IsDeniedBySiteFilter ? edgeEvaluation.DenialReason : $"this dependency goes the wrong direction — the reverse ('{depLayerName}' \u2192 '{callerLayerName}') is configured";
		}
		else
		{
			descriptor = ArchitecturalDiagnostics.IllegalDependency;
			diagnosticId = ArchitecturalDiagnosticIds.IllegalLevelDependency;
			reason = edgeEvaluation.DenialReason;
		}

		var properties = AddViolationProperties(
			ruleProperties.Add(ArchitecturalDiagnostics.PropertySite, site),
			callerTypeName,
			callerLayerName,
			depTypeName,
			depLayerName,
			reason,
			null);

		context.ReportDiagnostic(Diagnostic.Create(
			descriptor,
			reportLocation,
			properties,
			callerTypeName, callerLayerName, depTypeName, depLayerName, reason));

		violations.Add(new ViolationRecord(diagnosticId, callerTypeName, callerLayerName, depTypeName, depLayerName, reason, null));
	}

	private static ImmutableDictionary<string, string?> AddViolationProperties(ImmutableDictionary<string, string?> properties, string callerTypeName, string callerLayerName, string depTypeName, string depLayerName, string violationReason, string? comment) =>
		properties
			.SetItem(ArchitecturalDiagnostics.PropertyCallerTypeName, callerTypeName)
			.SetItem(ArchitecturalDiagnostics.PropertyCallerLayerName, callerLayerName)
			.SetItem(ArchitecturalDiagnostics.PropertyDepTypeName, depTypeName)
			.SetItem(ArchitecturalDiagnostics.PropertyDepLayerName, depLayerName)
			.SetItem(ArchitecturalDiagnostics.PropertyViolationReason, violationReason)
			.SetItem(ArchitecturalDiagnostics.PropertyComment, comment);

	/// <summary>
	///     Builds the rule-location property bag attached to ARCH001/003/004/005 diagnostics
	///     so the "Add to exceptions" code fix can find the originating
	///     matcher tag in <c>ArchitecturalLevels.xml</c>
	///     and knows which type name to insert as the new exception entry.
	/// </summary>
	private static ImmutableDictionary<string, string?> BuildRuleProperties(LayerMatch depMatch, string depTypeName)
	{
		var properties = ImmutableDictionary<string, string?>.Empty
			.Add(ArchitecturalDiagnostics.PropertyDepTypeName, depTypeName);

		if (depMatch.XmlLineNumber > 0)
		{
			properties = properties
				.Add(ArchitecturalDiagnostics.PropertyRuleXmlLine, depMatch.XmlLineNumber.ToString(System.Globalization.CultureInfo.InvariantCulture))
				.Add(ArchitecturalDiagnostics.PropertyRuleXmlCol, depMatch.XmlLinePosition.ToString(System.Globalization.CultureInfo.InvariantCulture))
				.Add(ArchitecturalDiagnostics.PropertyRuleXmlPath, depMatch.XmlPath);
		}

		return properties;
	}

	private static ImmutableDictionary<string, string?> BuildRuleProperties(MatcherRule rule, string depTypeName)
	{
		var properties = ImmutableDictionary<string, string?>.Empty.Add(ArchitecturalDiagnostics.PropertyDepTypeName, depTypeName);
		if (rule.XmlLineNumber > 0)
		{
			properties = properties
				.Add(ArchitecturalDiagnostics.PropertyRuleXmlLine, rule.XmlLineNumber.ToString(System.Globalization.CultureInfo.InvariantCulture))
				.Add(ArchitecturalDiagnostics.PropertyRuleXmlCol, rule.XmlLinePosition.ToString(System.Globalization.CultureInfo.InvariantCulture))
				.Add(ArchitecturalDiagnostics.PropertyRuleXmlPath, rule.XmlPath);
		}

		return properties;
	}

	/// <summary>
	///     Yields <paramref name="root" /> followed by every generic type argument and array
	///     element type, recursively. Duplicate symbols are visited only once.
	/// </summary>
	private static IEnumerable<ITypeSymbol> EnumerateTypeAndGenericArguments(ITypeSymbol root)
	{
		var visited = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
		var stack = new Stack<ITypeSymbol>();
		stack.Push(root);

		while (stack.Count > 0)
		{
			var current = stack.Pop();
			if (!visited.Add(current))
			{
				continue;
			}

			yield return current;

			if (current is INamedTypeSymbol named)
			{
				// Push in reverse so document order is preserved when popping.
				for (var i = named.TypeArguments.Length - 1; i >= 0; i--)
				{
					stack.Push(named.TypeArguments[i]);
				}
			}
			else if (current is IArrayTypeSymbol array)
			{
				stack.Push(array.ElementType);
			}
		}
	}

	/// <summary>
	///     Walks up the syntax tree to find the dotted namespace containing <paramref name="node" />.
	///     Uses syntax only — no semantic model required.
	/// </summary>
	private static string GetContainingNamespace(SyntaxNode node)
	{
		var parts = new List<string>();
		var current = node.Parent;

		while (current is not null)
		{
			if (current is NamespaceDeclarationSyntax nds)
			{
				parts.Add(nds.Name.ToString());
			}
			else if (current is FileScopedNamespaceDeclarationSyntax fsns)
			{
				parts.Add(fsns.Name.ToString());
			}

			current = current.Parent;
		}

		parts.Reverse();
		return string.Join(".", parts);
	}
}
