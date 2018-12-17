using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlitzLensLib
{
	public static class Extensions
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

		public static void SetUInt32(this byte[] self, int index, uint offset)
		{
			byte[] symBytes = BitConverter.GetBytes(offset);

			self[index + 0] = symBytes[0];
			self[index + 1] = symBytes[1];
			self[index + 2] = symBytes[2];
			self[index + 3] = symBytes[3];
		}
	}
}
