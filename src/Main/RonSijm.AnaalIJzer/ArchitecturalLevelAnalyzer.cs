using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using RonSijm.AnaalIJzer.Analysis;
using RonSijm.AnaalIJzer.Config;
using RonSijm.AnaalIJzer.Diagnostics;
using RonSijm.AnaalIJzer.Reporting;
using AnalyzerConfig = RonSijm.AnaalIJzer.Config.AnalyzerConfig;

namespace RonSijm.AnaalIJzer;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ArchitecturalLevelAnalyzer : DiagnosticAnalyzer
{
	// Forwarded for backward compatibility - tests and the code fix provider reference these.
	internal const string PropertyMatchedSuffix = ArchitecturalDiagnostics.PropertyMatchedSuffix;
	internal const string PropertyFixSuffix = ArchitecturalDiagnostics.PropertyFixSuffix;
	internal const string PropertyRuleXmlLine = ArchitecturalDiagnostics.PropertyRuleXmlLine;
	internal const string PropertyRuleXmlCol = ArchitecturalDiagnostics.PropertyRuleXmlCol;
	internal const string PropertyRuleXmlPath = ArchitecturalDiagnostics.PropertyRuleXmlPath;
	internal const string PropertyDepTypeName = ArchitecturalDiagnostics.PropertyDepTypeName;

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
	[
		ArchitecturalDiagnostics.IllegalDependency,
		ArchitecturalDiagnostics.UnrecognizedDependency,
		ArchitecturalDiagnostics.ForbiddenDependency,
		ArchitecturalDiagnostics.WrongDirectionDependency,
		ArchitecturalDiagnostics.SameLayerDependency,
		ArchitecturalDiagnostics.InvalidConfiguration,
		ArchitecturalDiagnostics.CyclicDependencyGraph,
	];

	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();

		context.RegisterCompilationStartAction(compilationContext =>
		{
			var config = ArchitecturalConfigParser.Parse(compilationContext.Options.AdditionalFiles, compilationContext.Compilation, compilationContext.CancellationToken);
			if (config.HasConfigurationIssues)
			{
				compilationContext.RegisterCompilationEndAction(context => ReportConfigurationIssues(context, config, compilationContext.Options.AdditionalFiles));
			}

			if (!config.HasLayers)
			{
				return;
			}

			var violations = new ConcurrentBag<ViolationRecord>();

			compilationContext.RegisterSyntaxNodeAction(ctx => LayerDependencyAnalyzer.AnalyzeConstructorDeclaration(ctx, config, violations), SyntaxKind.ConstructorDeclaration);

			compilationContext.RegisterSyntaxNodeAction(ctx => LayerDependencyAnalyzer.AnalyzeTypeDeclaration(ctx, config, violations), SyntaxKind.ClassDeclaration, SyntaxKind.StructDeclaration, SyntaxKind.InterfaceDeclaration, SyntaxKind.RecordDeclaration, SyntaxKind.RecordStructDeclaration);

			compilationContext.RegisterSyntaxNodeAction(ctx => LayerDependencyAnalyzer.AnalyzeMethodDeclaration(ctx, config, violations), SyntaxKind.MethodDeclaration);

			compilationContext.RegisterSyntaxNodeAction(ctx => LayerDependencyAnalyzer.AnalyzeFieldDeclaration(ctx, config, violations), SyntaxKind.FieldDeclaration);

			compilationContext.RegisterSyntaxNodeAction(ctx => LayerDependencyAnalyzer.AnalyzePropertyDeclaration(ctx, config, violations), SyntaxKind.PropertyDeclaration);

			compilationContext.RegisterSyntaxNodeAction(ctx => LayerDependencyAnalyzer.AnalyzeObjectCreation(ctx, config, violations), SyntaxKind.ObjectCreationExpression, SyntaxKind.ImplicitObjectCreationExpression);

			compilationContext.RegisterSyntaxNodeAction(ctx => LayerDependencyAnalyzer.AnalyzeInvocation(ctx, config, violations), SyntaxKind.InvocationExpression);

			compilationContext.RegisterSyntaxNodeAction(ctx => LayerDependencyAnalyzer.AnalyzeLocalDeclaration(ctx, config, violations), SyntaxKind.LocalDeclarationStatement);

			compilationContext.RegisterSyntaxNodeAction(ctx => LayerDependencyAnalyzer.AnalyzeAttribute(ctx, config, violations), SyntaxKind.Attribute);

			compilationContext.RegisterSyntaxNodeAction(ctx => LayerDependencyAnalyzer.AnalyzeStaticMemberAccess(ctx, config, violations), SyntaxKind.SimpleMemberAccessExpression);
		});
	}

	private static void ReportConfigurationIssues(CompilationAnalysisContext context, AnalyzerConfig config, ImmutableArray<AdditionalText> additionalFiles)
	{
		foreach (var issue in config.ConfigurationIssues)
		{
			var descriptor = issue.Kind == ConfigurationIssueKind.CyclicDependencyGraph
				? ArchitecturalDiagnostics.CyclicDependencyGraph
				: ArchitecturalDiagnostics.InvalidConfiguration;
			context.ReportDiagnostic(Diagnostic.Create(descriptor, CreateConfigurationLocation(issue, additionalFiles, context.CancellationToken), issue.Message));
		}
	}

	private static Location CreateConfigurationLocation(ConfigurationIssue issue, ImmutableArray<AdditionalText> additionalFiles, CancellationToken cancellationToken)
	{
		if (issue.LineNumber <= 0)
		{
			return Location.None;
		}

		var file = additionalFiles.FirstOrDefault(candidate => string.Equals(NormalizePath(candidate.Path), NormalizePath(issue.Path), StringComparison.OrdinalIgnoreCase));
		var text = file?.GetText(cancellationToken);
		if (text is null || issue.LineNumber > text.Lines.Count)
		{
			return Location.None;
		}

		var line = text.Lines[issue.LineNumber - 1];
		var character = Math.Max(0, Math.Min(issue.LinePosition - 1, line.Span.Length));
		var position = line.Start + character;
		return Location.Create(issue.Path, new Microsoft.CodeAnalysis.Text.TextSpan(position, 0), new Microsoft.CodeAnalysis.Text.LinePositionSpan(new Microsoft.CodeAnalysis.Text.LinePosition(issue.LineNumber - 1, character), new Microsoft.CodeAnalysis.Text.LinePosition(issue.LineNumber - 1, character)));
	}

	private static string NormalizePath(string path)
	{
		try
		{
			return Path.GetFullPath(path);
		}
		catch
		{
			return path;
		}
	}
}
