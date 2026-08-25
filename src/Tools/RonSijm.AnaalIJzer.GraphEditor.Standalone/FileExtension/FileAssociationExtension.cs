using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace RonSijm.AnaalIJzer.GraphEditor.Standalone.FileExtension;

public static class FileAssociationExtension
{
	private const int SHCNE_ASSOCCHANGED = 0x8000000;
	private const int SHCNF_FLUSH = 0x1000;

	[DllImport("Shell32.dll")]
	private static extern int SHChangeNotify(int eventId, int flags, IntPtr item1, IntPtr item2);

	public static bool CreateFileExtensionAssociation(this string extension, string programName, string fileTypeDescription, string inputCommand = "\"%1\"")
	{
		if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			var result = false;

			return result;
		}

		var mainModule = Process.GetCurrentProcess().MainModule;
		if (mainModule is null)
		{
			var result = false;

			return result;
		}

		var applicationFilePath = mainModule.FileName;
		var madeChanges = false;
		madeChanges |= SetKeyDefaultValue(@"Software\Classes\" + extension, programName);
		madeChanges |= SetKeyDefaultValue(@"Software\Classes\" + programName, fileTypeDescription);
		madeChanges |= SetKeyDefaultValue($@"Software\Classes\{programName}\shell\open\command", $"\"{applicationFilePath}\" {inputCommand}");

		if (madeChanges)
		{
			SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_FLUSH, IntPtr.Zero, IntPtr.Zero);
		}

		return madeChanges;
	}

	public static bool RemoveFileExtensionAssociation(this string extension, string programName)
	{
		if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			var result = false;

			return result;
		}

		var madeChanges = false;
		madeChanges |= DeleteExtensionKeyWhenOwnedByProgram(extension, programName);
		madeChanges |= DeleteKeyIfExists(@"Software\Classes\" + programName);

		if (madeChanges)
		{
			SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_FLUSH, IntPtr.Zero, IntPtr.Zero);
		}

		return madeChanges;
	}

	private static bool SetKeyDefaultValue(string keyPath, string value)
	{
		using var key = Registry.CurrentUser.CreateSubKey(keyPath);
		if (key is null)
		{
			var result = false;

			return result;
		}

		if (key.GetValue(null) as string == value)
		{
			var result = false;

			return result;
		}

		key.SetValue(null, value);

		return true;
	}

	private static bool DeleteExtensionKeyWhenOwnedByProgram(string extension, string programName)
	{
		var keyPath = @"Software\Classes\" + extension;
		using var key = Registry.CurrentUser.OpenSubKey(keyPath, writable: false);
		if (key?.GetValue(null) as string != programName)
		{
			var result = false;

			return result;
		}

		Registry.CurrentUser.DeleteSubKeyTree(keyPath, throwOnMissingSubKey: false);

		return true;
	}

	private static bool DeleteKeyIfExists(string keyPath)
	{
		using (var key = Registry.CurrentUser.OpenSubKey(keyPath, writable: false))
		{
			if (key is null)
			{
				var result = false;

				return result;
			}
		}

		Registry.CurrentUser.DeleteSubKeyTree(keyPath, throwOnMissingSubKey: false);

		return true;
	}
}
