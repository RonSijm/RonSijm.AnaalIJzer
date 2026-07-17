namespace RonSijm.AnaalIJzer.VisualStudio.Styling;

internal static class ArchitectureClassificationNames
{
	internal const string LayerTint01 = "AnaalIJzer Layer 01";
	internal const string LayerTint02 = "AnaalIJzer Layer 02";
	internal const string LayerTint03 = "AnaalIJzer Layer 03";
	internal const string LayerTint04 = "AnaalIJzer Layer 04";
	internal const string LayerTint05 = "AnaalIJzer Layer 05";
	internal const string LayerTint06 = "AnaalIJzer Layer 06";
	internal const string LayerTint07 = "AnaalIJzer Layer 07";
	internal const string LayerTint08 = "AnaalIJzer Layer 08";
	internal const string LayerTint09 = "AnaalIJzer Layer 09";
	internal const string LayerTint10 = "AnaalIJzer Layer 10";
	internal const string LayerTint11 = "AnaalIJzer Layer 11";
	internal const string LayerTint12 = "AnaalIJzer Layer 12";
	internal const string LayerTint13 = "AnaalIJzer Layer 13";
	internal const string LayerTint14 = "AnaalIJzer Layer 14";
	internal const string LayerTint15 = "AnaalIJzer Layer 15";
	internal const string LayerTint16 = "AnaalIJzer Layer 16";

	internal static string GetLayerTintName(int slot)
	{
		var result = slot switch
		{
			1 => LayerTint01,
			2 => LayerTint02,
			3 => LayerTint03,
			4 => LayerTint04,
			5 => LayerTint05,
			6 => LayerTint06,
			7 => LayerTint07,
			8 => LayerTint08,
			9 => LayerTint09,
			10 => LayerTint10,
			11 => LayerTint11,
			12 => LayerTint12,
			13 => LayerTint13,
			14 => LayerTint14,
			15 => LayerTint15,
			_ => LayerTint16
		};

		return result;
	}
}
