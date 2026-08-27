using System.Collections.Immutable;
using System.Reflection;
using RonSijm.AnaalIJzer.Arse.Components;
using RonSijm.AnaalIJzer.Core.Findings;

namespace RonSijm.AnaalIJzer.Arse.Tests.Components;

public sealed class AppExceptionInspectionTests
{
	[Fact]
	public async Task ExceptionInspectionFilters_HideDisabledStates()
	{
		var app = new App();
		SetPrivateField(
			app,
			"_inspectionFindings",
			ImmutableArray.Create(
				new ArchitectureFinding(ArchitectureFindingSeverity.Warning, "ARCH017", "Missing owner", "Architecture.anl:10", "Invalid"),
				new ArchitectureFinding(ArchitectureFindingSeverity.Warning, "ARCH017", "Expires soon", "Architecture.anl:12", "ExpiringSoon"),
				new ArchitectureFinding(ArchitectureFindingSeverity.Warning, "ARCH017", "Already expired", "Architecture.anl:14", "Expired"),
				new ArchitectureFinding(ArchitectureFindingSeverity.Warning, "ARCH017", "No longer matches code", "Architecture.anl:16", "Stale")));

		await InvokePrivateTaskMethod(app, "ToggleInspectionExceptionFilter", "Invalid");
		await InvokePrivateTaskMethod(app, "ToggleInspectionExceptionFilter", "Expired");

		var labels = GetFilteredInspectionFindings(app).Select(finding => finding.State).ToArray();

		labels.Should().Equal("ExpiringSoon", "Stale");
		GetPrivateMethod<string>(app, "GetInspectionExceptionFilterLabel", "Invalid").Should().Be("[ ] Invalid");
		GetPrivateMethod<string>(app, "GetInspectionExceptionFilterLabel", "ExpiringSoon").Should().Be("[x] ExpiringSoon");
	}

	private static ImmutableArray<ArchitectureFinding> GetFilteredInspectionFindings(App app)
	{
		var property = typeof(App).GetProperty("FilteredInspectionExceptionFindings", BindingFlags.Instance | BindingFlags.NonPublic)!;
		var result = (ImmutableArray<ArchitectureFinding>)property.GetValue(app)!;

		return result;
	}

	private static T GetPrivateMethod<T>(object instance, string methodName, params object[] arguments)
	{
		var result = (T)instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(instance, arguments)!;

		return result;
	}

	private static async Task InvokePrivateTaskMethod(object instance, string methodName, params object[] arguments)
	{
		var task = (Task)instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(instance, arguments)!;
		await task;
	}

	private static void SetPrivateField(object instance, string fieldName, object value)
	{
		instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(instance, value);
	}
}
