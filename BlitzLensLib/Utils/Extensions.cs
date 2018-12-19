using System;
using System.IO;
using System.Text;

namespace BlitzLensLib.Utils
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

		public static string Indent(this string self, int amount = 4)
		{
			StringBuilder sb = new StringBuilder();

			string s = self.Replace("\r", "");

			string[] lines = s.Split('\n');

			for (int i = 0; i < lines.Length; i++)
			{
				string line = lines[i];

				sb.AppendSpaces(amount);
				sb.Append(line);

				if (i < lines.Length - 1)
					sb.AppendLine();
			}

			return sb.ToString();
		}

		public static void AppendSpaces(this StringBuilder sb, int amount)
		{
			for (int i = 0; i < amount; i++)
				sb.Append(' ');
		}
	}
}
