using System.Collections;
using System.Collections.Immutable;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using AwesomeAssertions;
using Nodify;
using RonSijm.AnaalIJzer.Graphing.Model;
using RonSijm.AnaalIJzer.Graphing.ViewModels;

namespace RonSijm.AnaalIJzer.Graphing.Wpf.Tests.Controls;

public sealed partial class ArchitectureGraphCanvasTests
{
	private static void AssertConnectionsHaveResolvedAnchors(NodifyEditor editor)
	{
		foreach (var connection in editor.Connections.Cast<object>())
		{
			var outputAnchor = GetAnchor(connection, nameof(NodifyGraphConnectionProbe.Output));
			var inputAnchor = GetAnchor(connection, nameof(NodifyGraphConnectionProbe.Input));
			outputAnchor.Should().NotBe(new Point(), "Nodify outputs must publish their anchors so graph edges can render");
			inputAnchor.Should().NotBe(new Point(), "Nodify inputs must publish their anchors so graph edges can render");
			outputAnchor.Should().NotBe(inputAnchor, "a rendered edge should connect two distinct node anchors");
		}
	}

	private static Point GetAnchor(object connection, string connectorPropertyName)
	{
		var connector = connection.GetType().GetProperty(connectorPropertyName)!.GetValue(connection)!;
		var result = (Point)connector.GetType().GetProperty(nameof(NodifyGraphConnectorProbe.Anchor))!.GetValue(connector)!;

		return result;
	}

	private sealed class NodifyGraphConnectionProbe
	{
		public object? Output { get; set; }

		public object? Input { get; set; }
	}

	private sealed class NodifyGraphConnectorProbe
	{
		public Point Anchor { get; set; }
	}

	private static int CountRenderableEdges(ArchitectureGraphGroupViewModel group)
	{
		var endpointPaths = group.Nodes.Select(node => node.Path)
			.Concat(group.Boundaries.Select(boundary => boundary.Path))
			.ToHashSet(StringComparer.Ordinal);
		var result = group.Edges.Count(edge => endpointPaths.Contains(edge.From) && endpointPaths.Contains(edge.To));

		return result;
	}

	private static int Count(IEnumerable? source)
	{
		if (source is null)
		{
			return 0;
		}

		var result = source.Cast<object>().Count();

		return result;
	}

	private static object FindGraphItem(ImmutableArray<object> items, string path, string typeNamePart)
	{
		var result = items.Single(item => item.GetType().Name.Contains(typeNamePart, StringComparison.Ordinal) && string.Equals(GetProperty<string>(item, "Path"), path, StringComparison.Ordinal));

		return result;
	}

	private static T GetProperty<T>(object instance, string propertyName)
	{
		var result = (T)instance.GetType().GetProperty(propertyName)!.GetValue(instance)!;

		return result;
	}

	private static void SetProperty<T>(object instance, string propertyName, T value)
	{
		instance.GetType().GetProperty(propertyName)!.SetValue(instance, value);
	}

	private static T? FindVisualDescendant<T>(DependencyObject root) where T : DependencyObject
	{
		if (root is T match)
		{
			return match;
		}

		var childCount = VisualTreeHelper.GetChildrenCount(root);
		for (var index = 0; index < childCount; index++)
		{
			var child = VisualTreeHelper.GetChild(root, index);
			var result = FindVisualDescendant<T>(child);
			if (result is not null)
			{
				return result;
			}
		}

		return null;
	}

	private static IEnumerable<T> FindVisualDescendants<T>(DependencyObject root) where T : DependencyObject
	{
		if (root is T match)
		{
			yield return match;
		}

		var childCount = VisualTreeHelper.GetChildrenCount(root);
		for (var index = 0; index < childCount; index++)
		{
			var child = VisualTreeHelper.GetChild(root, index);
			foreach (var result in FindVisualDescendants<T>(child))
			{
				yield return result;
			}
		}
	}

	private static void RunOnStaThread(Action action)
	{
		ExceptionDispatchInfo? capturedException = null;
		var thread = new Thread(() =>
		{
			try
			{
				action();
			}
			catch (Exception exception)
			{
				capturedException = ExceptionDispatchInfo.Capture(exception);
			}
			finally
			{
				Dispatcher.CurrentDispatcher.InvokeShutdown();
			}
		});
		thread.SetApartmentState(ApartmentState.STA);
		thread.Start();
		thread.Join();
		capturedException?.Throw();
	}

	private static void DrainDispatcher()
	{
		var frame = new DispatcherFrame();
		Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => frame.Continue = false));
		Dispatcher.PushFrame(frame);
	}

	private static void RaiseComboBoxClick(ComboBox comboBox)
	{
		var args = new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left)
		{
			RoutedEvent = UIElement.PreviewMouseLeftButtonDownEvent,
			Source = comboBox
		};
		comboBox.RaiseEvent(args);
	}

	private static ArchitectureGraphSnapshot CreateSimpleSnapshot()
	{
		var layers = ImmutableArray.Create(
			new ArchitectureGraphLayer("Customer", "Customer", null, 0, 1, false),
			new ArchitectureGraphLayer("Waiter", "Waiter", null, 0, 2, true),
			new ArchitectureGraphLayer("Chef", "Chef", null, 0, 3, false));
		var rules = ImmutableArray.Create(
			new ArchitectureGraphRule("Customer", "Waiter", string.Empty, "AllowedDependency", "all sites", false, false, true),
			new ArchitectureGraphRule("Waiter", "Chef", string.Empty, "AllowedDependency", "all sites", false, false, true));
		var result = new ArchitectureGraphSnapshot(true, false, layers, rules, ImmutableArray.Create("Waiter"), ImmutableArray<string>.Empty);

		return result;
	}

	private static ArchitectureGraphSnapshot CreateComplexSnapshot()
	{
		var layers = ImmutableArray.Create(
			new ArchitectureGraphLayer("Application", "Application", "Application boundary", 0, 1, false),
			new ArchitectureGraphLayer("Application/Contracts", "Contracts", "Application public contracts", 1, 2, true),
			new ArchitectureGraphLayer("Application/Implementation", "Implementation", "Application implementation", 1, 3, false),
			new ArchitectureGraphLayer("DataAbstraction", "DataAbstraction", "Data boundary", 0, 4, false),
			new ArchitectureGraphLayer("DataAbstraction/Contracts", "Contracts", "Data contracts", 1, 5, false),
			new ArchitectureGraphLayer("DataAbstraction/Implementation", "Implementation", "Data implementation", 1, 6, false),
			new ArchitectureGraphLayer("Crosscutting", "Crosscutting", "Shared framework types", 0, 7, true),
			new ArchitectureGraphLayer("Diagnostics", "Diagnostics", "Disconnected graph", 0, 8, false));
		var rules = ImmutableArray.Create(
			new ArchitectureGraphRule("Application/Implementation", "Application/Contracts", "Application", "AllowedDependency", "Inheritance", false, false, true),
			new ArchitectureGraphRule("Application/Implementation", "DataAbstraction/Contracts", string.Empty, "AllowedDependency", "Constructor", false, false, true),
			new ArchitectureGraphRule("DataAbstraction/Implementation", "DataAbstraction/Contracts", "DataAbstraction", "AllowedDependency", "Inheritance", false, false, false),
			new ArchitectureGraphRule("DataAbstraction/Contracts", "Application/Implementation", string.Empty, "BlockedDependency", "all sites", false, false, false),
			new ArchitectureGraphRule("*", "Crosscutting", string.Empty, "AllowedDependency", "all sites", true, true, true));
		var result = new ArchitectureGraphSnapshot(
			true,
			false,
			layers,
			rules,
			ImmutableArray.Create("Application/Contracts", "Crosscutting"),
			ImmutableArray<string>.Empty);

		return result;
	}

	private static ArchitectureGraphSnapshot CreateParentEndpointSnapshot()
	{
		var layers = ImmutableArray.Create(
			new ArchitectureGraphLayer("Application", "Application", "Application boundary", 0, 1, false),
			new ArchitectureGraphLayer("Application/Contracts", "Contracts", "Application public contracts", 1, 2, true),
			new ArchitectureGraphLayer("Crosscutting", "Crosscutting", "Shared framework types", 0, 3, false));
		var rules = ImmutableArray.Create(new ArchitectureGraphRule("Application", "Crosscutting", string.Empty, "AllowedDependency", "all sites", false, false, true));
		var result = new ArchitectureGraphSnapshot(
			true,
			false,
			layers,
			rules,
			ImmutableArray.Create("Application/Contracts"),
			ImmutableArray<string>.Empty);

		return result;
	}
}
