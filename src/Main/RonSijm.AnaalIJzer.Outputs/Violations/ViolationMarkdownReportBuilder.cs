using System.Text;
using AnalyzerConfiguration = RonSijm.AnaalIJzer.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer.Violations;

internal static partial class ViolationMarkdownReportBuilder
{
	internal static string Generate(IEnumerable<ViolationRecord> violationBag, AnalyzerConfiguration config, string? inputName, string inputLabel)
	{
		var all = violationBag.ToList();
		var arch001 = all.Where(v => v.DiagnosticId == ArchitecturalDiagnosticIds.IllegalLevelDependency).OrderBy(v => v.CallerTypeName).ThenBy(v => v.DependencyTypeName).ToList();
		var arch002 = all.Where(v => v.DiagnosticId == ArchitecturalDiagnosticIds.UnrecognizedDependency).OrderBy(v => v.CallerTypeName).ThenBy(v => v.DependencyTypeName).ToList();
		var arch003 = all.Where(v => v.DiagnosticId == ArchitecturalDiagnosticIds.ForbiddenDependency).OrderBy(v => v.CallerTypeName).ThenBy(v => v.DependencyTypeName).ToList();
		var arch004 = all.Where(v => v.DiagnosticId == ArchitecturalDiagnosticIds.WrongDirectionDependency).OrderBy(v => v.CallerTypeName).ThenBy(v => v.DependencyTypeName).ToList();
		var arch005 = all.Where(v => v.DiagnosticId == ArchitecturalDiagnosticIds.SameLayerDependency).OrderBy(v => v.CallerTypeName).ThenBy(v => v.DependencyTypeName).ToList();
		var arch008 = all.Where(v => v.DiagnosticId == ArchitecturalDiagnosticIds.NameRuleViolation).OrderBy(v => v.CallerTypeName).ThenBy(v => v.DependencyTypeName).ToList();
		var arch009 = all.Where(v => v.DiagnosticId == ArchitecturalDiagnosticIds.ApiSurfaceLeakage).OrderBy(v => v.CallerLayerName).ThenBy(v => v.ApiMemberName).ThenBy(v => v.DependencyTypeName).ToList();
		var arch010 = all.Where(v => v.DiagnosticId == ArchitecturalDiagnosticIds.ProjectReferenceViolation).OrderBy(v => v.SourceProjectName).ThenBy(v => v.TargetProjectName).ToList();
		var arch011 = all.Where(v => v.DiagnosticId == ArchitecturalDiagnosticIds.PackageReferenceViolation).OrderBy(v => v.SourceProjectName).ThenBy(v => v.PackageId).ThenBy(v => v.PackageVersion).ToList();
		var arch012 = all.Where(v => v.DiagnosticId == ArchitecturalDiagnosticIds.VisibilityPolicyViolation).OrderBy(v => v.CallerLayerName).ThenBy(v => v.DependencyTypeName).ToList();
		var arch013 = all.Where(v => v.DiagnosticId == ArchitecturalDiagnosticIds.ContractPurityViolation).OrderBy(v => v.CallerLayerName).ThenBy(v => v.DependencyTypeName).ToList();
		var arch014 = all.Where(v => v.DiagnosticId == ArchitecturalDiagnosticIds.ForbiddenTransitiveExposure).OrderBy(v => v.CallerLayerName).ThenBy(v => v.ApiMemberName).ThenBy(v => v.ExposureDepth).ThenBy(v => v.ExposurePath).ToList();
		var arch015 = all.Where(v => v.DiagnosticId == ArchitecturalDiagnosticIds.SourceLocationViolation).OrderBy(v => v.CallerLayerName).ThenBy(v => v.CallerTypeName).ThenBy(v => v.NormalizedSourcePath).ToList();
		var arch016 = all.Where(v => v.DiagnosticId == ArchitecturalDiagnosticIds.BoundaryEntryPointViolation).OrderBy(v => v.BoundaryLayerName).ThenBy(v => v.CallerLayerName).ThenBy(v => v.CallerTypeName).ThenBy(v => v.DependencyTypeName).ToList();
		var arch018 = all.Where(v => v.DiagnosticId == ArchitecturalDiagnosticIds.ObservedDependencyCycle).OrderBy(v => v.CycleLayers).ThenBy(v => v.SourceProjectName).ToList();
		var arch019 = all.Where(v => v.DiagnosticId == ArchitecturalDiagnosticIds.InheritancePolicyViolation).OrderBy(v => v.CallerLayerName).ThenBy(v => v.DependencyTypeName).ToList();

		var sb = new StringBuilder();
		AppendHeader(sb, inputName, inputLabel);
		AppendSummary(sb, all.Count, arch001.Count, arch002.Count, arch003.Count, arch004.Count, arch005.Count, arch008.Count, arch009.Count, arch010.Count, arch011.Count, arch012.Count, arch013.Count, arch014.Count, arch015.Count, arch016.Count, arch018.Count, arch019.Count);
		if (all.Count == 0)
		{
			sb.AppendLine("✅ **No violations found.**");
			var noViolationsResult = sb.ToString();

			return noViolationsResult;
		}

		sb.AppendLine("---");
		sb.AppendLine();

		AppendArch001(sb, arch001);
		AppendArch004(sb, arch004);
		AppendArch005(sb, arch005);
		AppendArch002(sb, arch002);
		AppendArch003(sb, arch003);
		AppendArch008(sb, arch008);
		AppendArch009(sb, arch009);
		AppendArch010(sb, arch010);
		AppendArch011(sb, arch011);
		AppendArch012(sb, arch012);
		AppendArch013(sb, arch013);
		AppendArch014(sb, arch014);
		AppendArch015(sb, arch015);
		AppendArch016(sb, arch016);
		AppendArch018(sb, arch018);
		AppendArch019(sb, arch019);

		var result = sb.ToString();

		return result;
	}

	private static string EscapeTable(string value)
	{
		var result = value.Replace("|", "\\|").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\r", " ").Replace("\n", " ");

		return result;
	}
}
