using System.Collections.Immutable;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;
using RonSijm.AnaalIJzer.ConfigurationEditing.Editing;
using RonSijm.AnaalIJzer.ConfigurationEditing.Model;

namespace RonSijm.AnaalIJzer.GraphEditor.Wpf.Controls;

public sealed partial class ArchitectureGraphEditorControl
{
	private static readonly ImmutableArray<string> ApiSurfaceSiteNames =
	[
		"Constructor",
		"Method",
		"MethodReturn",
		"Field",
		"Property",
		"GenericArgument",
		"Inheritance",
		"InterfaceImplementation",
		"Attribute"
	];

	private void AddApiSurfaceEditors(StackPanel panel, ImmutableArray<ArchitectureConfigurationElementDetails> policies, ArchitectureLayerEditHandle handle)
	{
		panel.Children.Add(CreateSectionTitle("API exposure"));
		panel.Children.Add(CreateHintTextBlock("Controls what an externally visible declaration may expose. This is separate from permission to use a type internally.", new Thickness(0, 0, 0, 4)));
		if (policies.Length == 0)
		{
			panel.Children.Add(CreateHintTextBlock("None configured.", new Thickness(0, 0, 0, 4)));
		}

		foreach (var policy in policies)
		{
			panel.Children.Add(CreateApiSurfacePolicyEditor(policy));
		}

		panel.Children.Add(CreateNewApiSurfacePolicyEditor(handle));
	}
}
