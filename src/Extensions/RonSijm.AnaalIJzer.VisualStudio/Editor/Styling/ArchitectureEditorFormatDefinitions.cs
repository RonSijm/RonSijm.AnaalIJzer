using System.ComponentModel.Composition;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace RonSijm.AnaalIJzer.VisualStudio.Styling;

internal abstract class ArchitectureLayerTintFormatDefinition : MarkerFormatDefinition
{
	protected ArchitectureLayerTintFormatDefinition(byte red, byte green, byte blue)
	{
		BackgroundColor = Color.FromArgb(36, red, green, blue);
		ZOrder = 5;
	}
}

[Export(typeof(EditorFormatDefinition))]
[Name(ArchitectureClassificationNames.LayerTint01)]
[UserVisible(true)]
internal sealed class ArchitectureLayerTint01FormatDefinition : ArchitectureLayerTintFormatDefinition
{
	public ArchitectureLayerTint01FormatDefinition() : base(38, 111, 201) => DisplayName = ArchitectureClassificationNames.LayerTint01;
}

[Export(typeof(EditorFormatDefinition))]
[Name(ArchitectureClassificationNames.LayerTint02)]
[UserVisible(true)]
internal sealed class ArchitectureLayerTint02FormatDefinition : ArchitectureLayerTintFormatDefinition
{
	public ArchitectureLayerTint02FormatDefinition() : base(26, 136, 94) => DisplayName = ArchitectureClassificationNames.LayerTint02;
}

[Export(typeof(EditorFormatDefinition))]
[Name(ArchitectureClassificationNames.LayerTint03)]
[UserVisible(true)]
internal sealed class ArchitectureLayerTint03FormatDefinition : ArchitectureLayerTintFormatDefinition
{
	public ArchitectureLayerTint03FormatDefinition() : base(189, 116, 28) => DisplayName = ArchitectureClassificationNames.LayerTint03;
}

[Export(typeof(EditorFormatDefinition))]
[Name(ArchitectureClassificationNames.LayerTint04)]
[UserVisible(true)]
internal sealed class ArchitectureLayerTint04FormatDefinition : ArchitectureLayerTintFormatDefinition
{
	public ArchitectureLayerTint04FormatDefinition() : base(154, 85, 188) => DisplayName = ArchitectureClassificationNames.LayerTint04;
}

[Export(typeof(EditorFormatDefinition))]
[Name(ArchitectureClassificationNames.LayerTint05)]
[UserVisible(true)]
internal sealed class ArchitectureLayerTint05FormatDefinition : ArchitectureLayerTintFormatDefinition
{
	public ArchitectureLayerTint05FormatDefinition() : base(192, 64, 92) => DisplayName = ArchitectureClassificationNames.LayerTint05;
}

[Export(typeof(EditorFormatDefinition))]
[Name(ArchitectureClassificationNames.LayerTint06)]
[UserVisible(true)]
internal sealed class ArchitectureLayerTint06FormatDefinition : ArchitectureLayerTintFormatDefinition
{
	public ArchitectureLayerTint06FormatDefinition() : base(52, 138, 151) => DisplayName = ArchitectureClassificationNames.LayerTint06;
}

[Export(typeof(EditorFormatDefinition))]
[Name(ArchitectureClassificationNames.LayerTint07)]
[UserVisible(true)]
internal sealed class ArchitectureLayerTint07FormatDefinition : ArchitectureLayerTintFormatDefinition
{
	public ArchitectureLayerTint07FormatDefinition() : base(122, 116, 33) => DisplayName = ArchitectureClassificationNames.LayerTint07;
}

[Export(typeof(EditorFormatDefinition))]
[Name(ArchitectureClassificationNames.LayerTint08)]
[UserVisible(true)]
internal sealed class ArchitectureLayerTint08FormatDefinition : ArchitectureLayerTintFormatDefinition
{
	public ArchitectureLayerTint08FormatDefinition() : base(88, 117, 178) => DisplayName = ArchitectureClassificationNames.LayerTint08;
}

[Export(typeof(EditorFormatDefinition))]
[Name(ArchitectureClassificationNames.LayerTint09)]
[UserVisible(true)]
internal sealed class ArchitectureLayerTint09FormatDefinition : ArchitectureLayerTintFormatDefinition
{
	public ArchitectureLayerTint09FormatDefinition() : base(64, 148, 79) => DisplayName = ArchitectureClassificationNames.LayerTint09;
}

[Export(typeof(EditorFormatDefinition))]
[Name(ArchitectureClassificationNames.LayerTint10)]
[UserVisible(true)]
internal sealed class ArchitectureLayerTint10FormatDefinition : ArchitectureLayerTintFormatDefinition
{
	public ArchitectureLayerTint10FormatDefinition() : base(176, 77, 42) => DisplayName = ArchitectureClassificationNames.LayerTint10;
}

[Export(typeof(EditorFormatDefinition))]
[Name(ArchitectureClassificationNames.LayerTint11)]
[UserVisible(true)]
internal sealed class ArchitectureLayerTint11FormatDefinition : ArchitectureLayerTintFormatDefinition
{
	public ArchitectureLayerTint11FormatDefinition() : base(135, 94, 58) => DisplayName = ArchitectureClassificationNames.LayerTint11;
}

[Export(typeof(EditorFormatDefinition))]
[Name(ArchitectureClassificationNames.LayerTint12)]
[UserVisible(true)]
internal sealed class ArchitectureLayerTint12FormatDefinition : ArchitectureLayerTintFormatDefinition
{
	public ArchitectureLayerTint12FormatDefinition() : base(84, 128, 130) => DisplayName = ArchitectureClassificationNames.LayerTint12;
}

[Export(typeof(EditorFormatDefinition))]
[Name(ArchitectureClassificationNames.LayerTint13)]
[UserVisible(true)]
internal sealed class ArchitectureLayerTint13FormatDefinition : ArchitectureLayerTintFormatDefinition
{
	public ArchitectureLayerTint13FormatDefinition() : base(177, 83, 139) => DisplayName = ArchitectureClassificationNames.LayerTint13;
}

[Export(typeof(EditorFormatDefinition))]
[Name(ArchitectureClassificationNames.LayerTint14)]
[UserVisible(true)]
internal sealed class ArchitectureLayerTint14FormatDefinition : ArchitectureLayerTintFormatDefinition
{
	public ArchitectureLayerTint14FormatDefinition() : base(91, 121, 57) => DisplayName = ArchitectureClassificationNames.LayerTint14;
}

[Export(typeof(EditorFormatDefinition))]
[Name(ArchitectureClassificationNames.LayerTint15)]
[UserVisible(true)]
internal sealed class ArchitectureLayerTint15FormatDefinition : ArchitectureLayerTintFormatDefinition
{
	public ArchitectureLayerTint15FormatDefinition() : base(55, 132, 192) => DisplayName = ArchitectureClassificationNames.LayerTint15;
}

[Export(typeof(EditorFormatDefinition))]
[Name(ArchitectureClassificationNames.LayerTint16)]
[UserVisible(true)]
internal sealed class ArchitectureLayerTint16FormatDefinition : ArchitectureLayerTintFormatDefinition
{
	public ArchitectureLayerTint16FormatDefinition() : base(128, 100, 194) => DisplayName = ArchitectureClassificationNames.LayerTint16;
}
