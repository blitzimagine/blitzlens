using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BlitzLensLib.Utils
{
	// From: https://stackoverflow.com/questions/45624557/c-sharp-extract-resource-from-native-pe
	internal static class ResourceHelper
	{
		[Flags]
		[SuppressMessage("ReSharper", "InconsistentNaming")]
		[SuppressMessage("ReSharper", "UnusedMember.Local")]
		[SuppressMessage("ReSharper", "UnusedMember.Global")]
		private enum LoadLibraryFlags : uint
		{
			None = 0,
			DONT_RESOLVE_DLL_REFERENCES = 0x00000001,
			LOAD_IGNORE_CODE_AUTHZ_LEVEL = 0x00000010,
			LOAD_LIBRARY_AS_DATAFILE = 0x00000002,
			LOAD_LIBRARY_AS_DATAFILE_EXCLUSIVE = 0x00000040,
			LOAD_LIBRARY_AS_IMAGE_RESOURCE = 0x00000020,
			LOAD_LIBRARY_SEARCH_APPLICATION_DIR = 0x00000200,
			LOAD_LIBRARY_SEARCH_DEFAULT_DIRS = 0x00001000,
			LOAD_LIBRARY_SEARCH_DLL_LOAD_DIR = 0x00000100,
			LOAD_LIBRARY_SEARCH_SYSTEM32 = 0x00000800,
			LOAD_LIBRARY_SEARCH_USER_DIRS = 0x00000400,
			LOAD_WITH_ALTERED_SEARCH_PATH = 0x00000008
		}

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hReservedNull, LoadLibraryFlags dwFlags);

		[DllImport("Kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern IntPtr LoadLibrary(string lpFileName);

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool FreeLibrary(IntPtr hLibModule);

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern IntPtr FindResource(IntPtr hModule, string lpName, string lpType);

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern IntPtr LoadResource(IntPtr hModule, IntPtr hResInfo);

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern IntPtr LockResource(IntPtr hResData);

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern uint SizeofResource(IntPtr hModule, IntPtr hResInfo);

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool EnumResourceNames(IntPtr hModule, string lpType, IntPtr lpEnumFunc, IntPtr lParam);

		public static byte[] GetResourceFromExecutable(string lpFileName, string lpName, string lpType)
		{
			IntPtr hModule = LoadLibraryEx(lpFileName, IntPtr.Zero, LoadLibraryFlags.LOAD_LIBRARY_AS_DATAFILE);
			if (hModule != IntPtr.Zero)
			{
				IntPtr hResource = FindResource(hModule, lpName, lpType);
				if (hResource != IntPtr.Zero)
				{
					uint resSize = SizeofResource(hModule, hResource);
					IntPtr resData = LoadResource(hModule, hResource);
					if (resData != IntPtr.Zero)
					{
						byte[] uiBytes = new byte[resSize];
						IntPtr ipMemorySource = LockResource(resData);
						Marshal.Copy(ipMemorySource, uiBytes, 0, (int)resSize);
						FreeLibrary(hModule);
						return uiBytes;
					}
				}

				FreeLibrary(hModule);
			}
			return null;
		}

		public static byte[] GetBlitzCodeFromExecutable(string filename)
		{
			return ResourceHelper.GetResourceFromExecutable(filename, "#1111", "#10");
		}
	}
}
