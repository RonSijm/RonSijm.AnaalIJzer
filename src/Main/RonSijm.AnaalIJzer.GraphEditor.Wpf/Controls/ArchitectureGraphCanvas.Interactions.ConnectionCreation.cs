using Microsoft.Extensions.Logging;
using RonSijm.AnaalIJzer.ConfigurationEditing.Editing;

namespace RonSijm.AnaalIJzer.GraphEditor.Wpf.Controls;

internal sealed partial class ArchitectureGraphCanvas
{
	private void CompleteConnection(object? parameter)
	{
		try
		{
			if (!TryGetConnectionEndpoints(parameter, out var from, out var to))
			{
				_logger?.LogWarning("Could not resolve connection endpoints from Nodify parameter of type {ParameterType}.", parameter?.GetType().FullName ?? "<null>");
				ReportEditResult(ArchitectureConfigurationEditResult.Failure("Drag from an out connector to an in connector to add an AllowedDependency."));
				return;
			}

			if (string.Equals(from, to, StringComparison.Ordinal))
			{
				_logger?.LogInformation("Rejected self-connection for layer '{LayerPath}'.", from);
				ReportEditResult(ArchitectureConfigurationEditResult.Failure("A layer cannot be connected to itself from the graph editor."));
				return;
			}

			_logger?.LogInformation("Adding AllowedDependency from '{From}' to '{To}' from graph gesture.", from, to);
			var result = _editService.AddAllowedDependency(_group.ConfigurationSource, from, to);
			ReportEditResult(result);
		}
		catch (Exception exception)
		{
			_logger?.LogError(exception, "Failed to complete graph connection gesture.");
			ReportEditResult(ArchitectureConfigurationEditResult.Failure("Adding the dependency failed. See the graph editor log for details."));
		}
	}

	private static bool TryGetConnectionEndpoints(object? parameter, out string from, out string to)
	{
		if (!TryGetTupleItems(parameter, out var first, out var second))
		{
			from = string.Empty;
			to = string.Empty;
			return false;
		}

		if (first is NodifyGraphConnectorViewModel firstConnector && second is NodifyGraphConnectorViewModel secondConnector)
		{
			return TryGetConnectionEndpoints(firstConnector, secondConnector, out from, out to);
		}

		from = string.Empty;
		to = string.Empty;
		return false;
	}

	private static bool TryGetTupleItems(object? parameter, out object? first, out object? second)
	{
		if (parameter is Tuple<object, object> tuple)
		{
			first = tuple.Item1;
			second = tuple.Item2;
			return true;
		}

		var type = parameter?.GetType();
		var firstProperty = type?.GetProperty("Item1");
		var secondProperty = type?.GetProperty("Item2");
		if (parameter is not null && firstProperty is not null && secondProperty is not null)
		{
			first = firstProperty.GetValue(parameter);
			second = secondProperty.GetValue(parameter);
			return true;
		}

		first = null;
		second = null;
		return false;
	}

	private static bool TryGetConnectionEndpoints(NodifyGraphConnectorViewModel first, NodifyGraphConnectorViewModel second, out string from, out string to)
	{
		if (first.IsOutput && second.IsInput)
		{
			from = first.LayerPath;
			to = second.LayerPath;
			return true;
		}

		if (first.IsInput && second.IsOutput)
		{
			from = second.LayerPath;
			to = first.LayerPath;
			return true;
		}

		from = string.Empty;
		to = string.Empty;
		return false;
	}
}
