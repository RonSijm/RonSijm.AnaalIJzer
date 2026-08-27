using System.Collections.Immutable;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using AwesomeAssertions;
using Nodify;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;
using RonSijm.AnaalIJzer.GraphEditor.Wpf.Controls;
using RonSijm.AnaalIJzer.GraphModel.Loading;
using RonSijm.AnaalIJzer.GraphModel.Model;

namespace RonSijm.AnaalIJzer.GraphEditor.Wpf.Tests.Controls;

public sealed partial class ArchitectureGraphEditorControlPersistenceTests
{
	private static ArchitectureGraphEditorControl CreateControl(
		ArchitectureGraphSnapshot snapshot,
		Func<ArchitectureGraphSnapshot, ArchitectureGraphSnapshot>? snapshotReloader = null,
		Func<string, bool>? confirmationHandler = null,
		Func<ArchitectureLayerCreationRequest?>? layerCreationHandler = null)
	{
		var control = new ArchitectureGraphEditorControl(
			snapshot: snapshot,
			focusMode: ArchitectureGraphFocusMode.ShowAll,
			theme: null,
			infoLogger: null,
			warningLogger: null,
			logger: null,
			editService: null,
			snapshotReloader: snapshotReloader,
			snapshotPublisher: null,
			confirmationHandler: confirmationHandler,
			layerCreationHandler: layerCreationHandler);
		control.Measure(new Size(1280, 860));
		control.Arrange(new Rect(0, 0, 1280, 860));
		control.UpdateLayout();

		return control;
	}

	private static ArchitectureGraphSnapshot CreateSnapshot(string sourcePath, ArchitectureConfigurationSourceKind sourceKind)
	{
		var source = new ArchitectureConfigurationSource(sourceKind, sourcePath);
		var layers = ImmutableArray.Create(
			new ArchitectureGraphLayer("Customer", "Customer", null, 0, 1, false, sourcePath, sourceKind),
			new ArchitectureGraphLayer("Waiter", "Waiter", null, 0, 2, false, sourcePath, sourceKind));
		var rules = ImmutableArray.Create(
			new ArchitectureGraphRule(
				"Customer",
				"Waiter",
				string.Empty,
				"AllowedDependency",
				"all sites",
				false,
				false,
				false,
				sourcePath: sourcePath,
				sourceKind: sourceKind));
		var result = new ArchitectureGraphSnapshot(
			true,
			false,
			layers,
			rules,
			ImmutableArray<string>.Empty,
			ImmutableArray<string>.Empty,
			source);

		return result;
	}

	private static string WriteTempFile(string fileName, string content)
	{
		var directory = Path.Combine(Path.GetTempPath(), "AnaalIJzerGraphEditorTests", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(directory);
		var path = Path.Combine(directory, fileName);
		File.WriteAllText(path, content);

		return path;
	}

	private static string WriteInlineConfigurationFile(string xml)
	{
		var source = "using System.Reflection;" + Environment.NewLine
		             + Environment.NewLine
		             + "[assembly: AssemblyMetadata(\"AnaalIJzerSettings\", \"\"\"" + Environment.NewLine
		             + xml.Trim()
		             + Environment.NewLine
		             + "\"\"\")]" + Environment.NewLine;
		var result = WriteTempFile("Example.cs", source);

		return result;
	}

	private static string WriteInterpolatedInlineConfigurationFile(string xml, string code)
	{
		var source = "using System.Reflection;" + Environment.NewLine
		             + Environment.NewLine
		             + "[assembly: AssemblyMetadata(\"AnaalIJzerSettings\", $\"\"\"" + Environment.NewLine
		             + xml.Trim()
		             + Environment.NewLine
		             + "\"\"\")]" + Environment.NewLine
		             + code.Trim()
		             + Environment.NewLine;
		var result = WriteTempFile("Example.cs", source);

		return result;
	}

	private static ArchitectureGraphSnapshot LoadInlineSnapshot(string path)
	{
		var source = new ArchitectureConfigurationSource(ArchitectureConfigurationSourceKind.InlineAssemblyMetadata, path);
		var result = ArchitectureGraphXmlSnapshotLoader.Load(source);

		return result;
	}

	private static object FindGraphItemDataContext(DependencyObject root, string path)
	{
		var result = FindVisualDescendants<Node>(root)
			.Select(node => node.DataContext)
			.Concat(FindVisualDescendants<GroupingNode>(root).Select(boundary => boundary.DataContext))
			.Single(item => string.Equals(GetText(GetObjectProperty(item, "Path")), path, StringComparison.Ordinal));

		return result;
	}

	private static TextBox FindTextBoxByText(DependencyObject root, string text)
	{
		var result = FindVisualDescendants<TextBox>(root).FirstOrDefault(textBox => textBox.Text == text);
		result.Should().NotBeNull("the graph editor should render an editable textbox containing '" + text + "'");

		return result;
	}

	private static string? GetDataContextProperty(FrameworkElement element, string propertyName)
	{
		var result = element.DataContext?.GetType().GetProperty(propertyName)?.GetValue(element.DataContext)?.ToString();

		return result;
	}

	private static void SetObjectProperty(object instance, string propertyName, object value)
	{
		instance.GetType().GetProperty(propertyName)!.SetValue(instance, value);
	}

	private static object? GetObjectProperty(object? instance, string propertyName)
	{
		var result = instance?.GetType().GetProperty(propertyName)?.GetValue(instance);

		return result;
	}

	private static T FindVisualDescendant<T>(DependencyObject root) where T : DependencyObject
	{
		var result = FindVisualDescendants<T>(root).First();

		return result;
	}

	private static CheckBox FindCheckBoxByContent(DependencyObject root, string content)
	{
		var result = FindVisualDescendants<CheckBox>(root).FirstOrDefault(checkBox => string.Equals(GetText(checkBox.Content), content, StringComparison.Ordinal));
		result.Should().NotBeNull("the graph editor should render a checkbox named '" + content + "'");

		return result;
	}

	private static Expander FindExpanderByHeader(DependencyObject root, string header)
	{
		var result = FindVisualDescendants<Expander>(root).FirstOrDefault(expander => string.Equals(GetText(expander.Header), header, StringComparison.Ordinal));
		result.Should().NotBeNull("the graph editor should render an expander named '" + header + "'");

		return result;
	}

	private static MenuItem FindMenuItemByHeader(ItemCollection items, string header)
	{
		var result = items.OfType<MenuItem>().Single(item => string.Equals(GetText(item.Header), header, StringComparison.Ordinal));

		return result;
	}

	private static string? GetText(object? value)
	{
		var result = value as string ?? value?.ToString();

		return result;
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
			foreach (var descendant in FindVisualDescendants<T>(child))
			{
				yield return descendant;
			}
		}
	}

	private static void DrainDispatcher()
	{
		var frame = new DispatcherFrame();
		Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => frame.Continue = false));
		Dispatcher.PushFrame(frame);
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
}
