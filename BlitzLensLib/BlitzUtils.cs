using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlitzLensLib
{
	public static class BlitzUtils
	{
		public static byte[] GetBlitzCodeFromExecutable(string filename)
		{
			return ResourceHelper.GetResourceFromExecutable(filename, "#1111", "#10");
		}
	}
}
