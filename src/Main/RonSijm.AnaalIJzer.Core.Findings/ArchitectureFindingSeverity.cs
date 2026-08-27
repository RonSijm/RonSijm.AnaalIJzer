using Microsoft.CodeAnalysis;

namespace RonSijm.AnaalIJzer.Core.Findings;

public enum ArchitectureFindingSeverity
{
	Info,
	Warning,
	Error
}

public static class ArchitectureFindingSeverityExtensions
{
	public static string ToDisplayText(this ArchitectureFindingSeverity severity)
	{
		var result = severity switch
		{
			ArchitectureFindingSeverity.Info => "Info",
			ArchitectureFindingSeverity.Warning => "Warning",
			_ => "Error"
		};

		return result;
	}

	public static ArchitectureFindingSeverity FromDiagnosticSeverity(DiagnosticSeverity severity)
	{
		var result = severity switch
		{
			DiagnosticSeverity.Info or DiagnosticSeverity.Hidden => ArchitectureFindingSeverity.Info,
			DiagnosticSeverity.Warning => ArchitectureFindingSeverity.Warning,
			_ => ArchitectureFindingSeverity.Error
		};

		return result;
	}
}
