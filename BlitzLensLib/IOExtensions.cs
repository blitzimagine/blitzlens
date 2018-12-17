using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlitzLensLib
{
	public static class IOExtensions
	{
		public static string ReadCString(this BinaryReader br)
		{
			StringBuilder sb = new StringBuilder();

			while (!br.Eof())
			{
				byte b = br.ReadByte();
				if (b == 0)
					break;
				sb.Append((char)b);
			}

			return sb.ToString();
		}

		public static bool Eof(this Stream s)
		{
			return s.Position >= s.Length;
		}

		public static bool Eof(this BinaryReader br)
		{
			return br.BaseStream.Eof();
		}
	}
}
