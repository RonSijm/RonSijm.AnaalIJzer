using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32;

namespace RonSijm.AnaalIJzer.Arse.FileExtension;

public static class FileAssociationExtension
{
	private const int SHCNE_ASSOCCHANGED = 0x8000000;
	private const int SHCNF_FLUSH = 0x1000;

	[DllImport("Shell32.dll")]
	private static extern int SHChangeNotify(int eventId, int flags, IntPtr item1, IntPtr item2);

	public static bool CreateFileExtensionAssociation(this string extension, string programName, string fileTypeDescription, string inputCommand = "\"%1\"")
	{
		if (!OperatingSystem.IsWindows())
		{
			return false;
		}

		var mainModule = Process.GetCurrentProcess().MainModule;
		if (mainModule is null)
		{
			return false;
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
		if (!OperatingSystem.IsWindows())
		{
			return false;
		}

		var madeChanges = false;
		using (var extensionKey = Registry.CurrentUser.OpenSubKey(@"Software\Classes\" + extension, writable: true))
		{
			if (extensionKey?.GetValue(string.Empty) as string == programName)
			{
				extensionKey.DeleteValue(string.Empty, throwOnMissingValue: false);
				madeChanges = true;
			}
		}

		using (var classesKey = Registry.CurrentUser.OpenSubKey(@"Software\Classes", writable: true))
		{
			if (classesKey?.OpenSubKey(programName) is not null)
			{
				classesKey.DeleteSubKeyTree(programName, throwOnMissingSubKey: false);
				madeChanges = true;
			}
		}

		if (madeChanges)
		{
			SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_FLUSH, IntPtr.Zero, IntPtr.Zero);
		}

		return madeChanges;
	}

	[SupportedOSPlatform("windows")]
	private static bool SetKeyDefaultValue(string keyPath, string value)
	{
		using var key = Registry.CurrentUser.CreateSubKey(keyPath);
		if (key is null)
		{
			return false;
		}

		if (key.GetValue(string.Empty) as string == value)
		{
			return false;
		}

		key.SetValue(string.Empty, value);

		return true;
	}
}
