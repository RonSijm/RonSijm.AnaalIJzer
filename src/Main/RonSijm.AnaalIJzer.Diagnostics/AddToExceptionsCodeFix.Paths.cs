namespace RonSijm.AnaalIJzer.Diagnostics;

internal static partial class AddToExceptionsCodeFix
{
	private static bool IsSamePath(string left, string right)
	{
		try
		{
			return string.Equals(Path.GetFullPath(left), Path.GetFullPath(right), StringComparison.OrdinalIgnoreCase);
		}
		catch
		{
			return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
		}
	}
}
