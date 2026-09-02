using System.Text;

namespace RonSijm.AnaalIJzer.Outputs.Violations;

internal static partial class ViolationMarkdownReportBuilder
{
	private static void AppendHeader(StringBuilder sb, string? inputName, string inputLabel)
	{
		sb.AppendLine("# Architectural Violation Report");
		sb.AppendLine();
		if (inputName is not null)
		{
			sb.AppendLine($"**{inputLabel}**: `{inputName}`  ");
		}

		sb.AppendLine($"**Generated**: {DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}");
		sb.AppendLine();
		sb.AppendLine("---");
		sb.AppendLine();
	}

	private static void AppendSummary(StringBuilder sb, int total, int arch001, int arch002, int arch003, int arch004, int arch005, int arch008, int arch009, int arch010, int arch011, int arch012, int arch013, int arch014, int arch015, int arch016, int arch018, int arch019, int arch020)
	{
		sb.AppendLine("## Summary");
		sb.AppendLine();
		sb.AppendLine("| Rule | Violations |");
		sb.AppendLine("|------|------------|");
		sb.AppendLine($"| ARCH001 — Illegal layer dependency | {arch001} |");
		sb.AppendLine($"| ARCH002 — Unrecognized dependency | {arch002} |");
		sb.AppendLine($"| ARCH003 — Type policy violation | {arch003} |");
		sb.AppendLine($"| ARCH004 — Wrong-direction dependency | {arch004} |");
		sb.AppendLine($"| ARCH005 — Same-layer dependency | {arch005} |");
		sb.AppendLine($"| ARCH008 — Name rule violation | {arch008} |");
		sb.AppendLine($"| ARCH009 — API surface leakage | {arch009} |");
		sb.AppendLine($"| ARCH010 — Project reference violation | {arch010} |");
		sb.AppendLine($"| ARCH011 — Package reference violation | {arch011} |");
		sb.AppendLine($"| ARCH012 — Visibility policy violation | {arch012} |");
		sb.AppendLine($"| ARCH013 — Contract purity violation | {arch013} |");
		sb.AppendLine($"| ARCH014 — Forbidden transitive exposure | {arch014} |");
		sb.AppendLine($"| ARCH015 — Source-location violation | {arch015} |");
		sb.AppendLine($"| ARCH016 — Boundary entry-point violation | {arch016} |");
		sb.AppendLine($"| ARCH018 — Observed dependency cycle | {arch018} |");
		sb.AppendLine($"| ARCH019 — Inheritance policy violation | {arch019} |");
		sb.AppendLine($"| ARCH020 — Return-value policy violation | {arch020} |");
		sb.AppendLine($"| **Total** | **{total}** |");
		sb.AppendLine();
	}
}
