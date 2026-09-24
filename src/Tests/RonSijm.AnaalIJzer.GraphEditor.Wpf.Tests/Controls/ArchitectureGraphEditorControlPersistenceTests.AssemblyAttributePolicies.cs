using System.IO;
using System.Windows;
using AwesomeAssertions;
using RonSijm.AnaalIJzer.GraphModel.Loading;
using Xunit;

namespace RonSijm.AnaalIJzer.GraphEditor.Wpf.Tests.Controls;

public sealed partial class ArchitectureGraphEditorControlPersistenceTests
{
    [Fact]
    public void RootInspector_ShowsAndPersistsAssemblyAttributePolicies()
    {
        RunOnStaThread(() =>
        {
            var path = WriteTempFile(
                "Architecture.anl",
                """
				<ArchitecturalLevels>
				  <AssemblyAttributePolicy description="Friend access stays reviewed.">
				    <Forbidden>
				      <Attribute exactFullName="System.Runtime.CompilerServices.InternalsVisibleToAttribute">
				        <Argument index="0" exactName="NotAllowedExample" />
				      </Attribute>
				    </Forbidden>
				  </AssemblyAttributePolicy>
				</ArchitecturalLevels>
				""");
            var control = CreateControl(ArchitectureGraphXmlSnapshotLoader.Load(path), _ => ArchitectureGraphXmlSnapshotLoader.Load(path));

            GetVisualText(control).Should().Contain("Assembly attribute policies");
            var policy = FindExpanderByHeader(control, "<AssemblyAttributePolicy description=\"Friend access stays reviewed.\" />");
            policy.IsExpanded = true;
            DrainDispatcher();
            var description = FindTextBoxByText(control, "Friend access stays reviewed.");

            description.Text = "Friend access requires review.";
            description.RaiseEvent(new RoutedEventArgs(UIElement.LostFocusEvent));
            DrainDispatcher();

            File.ReadAllText(path).Should().Contain("description=\"Friend access requires review.\"");
        });
    }
}