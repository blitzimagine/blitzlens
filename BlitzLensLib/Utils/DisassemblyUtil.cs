using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BlitzLensLib.Structures;
using SharpDisasm;
using SharpDisasm.Udis86;

namespace BlitzLensLib.Utils
{
	internal static class DisassemblyUtil
	{
		public static byte[] TrimNops(string name, byte[] data)
		{
			// Only string variables can have nops at the end in Blitz
			if (!IsString(name))
				return data;

			List<byte> result = new List<byte>();

			int length = data.Length;
			for (int i = data.Length - 1; i >= 0; i--)
			{
				byte b = data[i];
				if (b == 0x90)
					length--;
			}

			for (int i = 0; i < length; i++)
			{
				result.Add(data[i]);
			}

			result.Reverse();

			return result.ToArray();
		}

		public static bool IsDigitString(string s)
		{
			foreach (char c in s)
			{
				if (!char.IsDigit(c))
					return false;
			}

			return true;
		}

		public static bool IsString(string name)
		{
			// All and Only string variable names are an underscore followed by a decimal number
			if (name.Length <= 1)
				return false;
			return name.StartsWith("_") && IsDigitString(name.Substring(1));
		}

		public static string GetString(byte[] data)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append(".db \"");
			for (int i = 0; i < data.Length; i++)
			{
				if (data[i] == 0)
					break;
				if (data[i] == '\\')
					sb.Append('\\');
				sb.Append((char) data[i]);
			}

			sb.Append("\", 0x00");

			return sb.ToString();
		}

		public static bool IsArray(string name)
		{
			return name.StartsWith("_a") && name.Length > 2;
		}

		public static string GetArrayString(byte[] data)
		{
			StringBuilder sb = new StringBuilder();

			using (MemoryStream ms = new MemoryStream(data))
			using (BinaryReader br = new BinaryReader(ms))
			{
				uint ptr = br.ReadUInt32();
				sb.AppendLine("    .dd 0x" + ptr.ToString("X8") + " ; Pointer");
				DataType type = (DataType) br.ReadInt32();
				sb.AppendLine("    .dd 0x" + ((int) type).ToString("X2") + " ; Type: " + type);
				int dimensions = br.ReadInt32();
				sb.AppendLine("    .dd 0x" + dimensions.ToString("X2") + " ; Dimensions: " + dimensions);
				int scales = br.ReadInt32();
				sb.AppendLine("    .dd 0x" + scales.ToString("X2") + " ; Scales");
			}

			return sb.ToString();
		}

		public static string GetByteArrayString(byte[] data)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append(".db ");
			for (int i = 0; i < data.Length; i++)
			{
				sb.Append("0x" + data[i].ToString("X2"));

				if (i < data.Length - 1)
					sb.Append(", ");
			}

			return sb.ToString();
		}

		public static string DisassembleLibsVar(byte[] data, CodeResource code,
			ref Dictionary<string, Dictionary<string, string>> libs)
		{
			StringBuilder sb = new StringBuilder();

			using (MemoryStream ms = new MemoryStream(data))
			using (BinaryReader br = new BinaryReader(ms))
			{
				while (!br.Eof())
				{
					var dllName = br.ReadCString();
					if (string.IsNullOrWhiteSpace(dllName))
					{
						sb.Append("    .db 0x00");
						break;
					}

					sb.AppendLine("    .db \"" + dllName.Replace("\\", "\\\\") + "\", 0x00");

					libs.Add(dllName, new Dictionary<string, string>());

					while (!br.Eof())
					{
						var funcName = br.ReadCString();
						if (string.IsNullOrWhiteSpace(funcName))
						{
							sb.AppendLine("    .db 0x00");
							break;
						}

						sb.AppendLine("    .db \"" + funcName.Replace("\\", "\\\\") + "\", 0x00");

						uint offset = br.ReadUInt32();
						string sym = code.GetSymbolName(offset);
						if (sym == null)
						{
							sym = "0x" + offset.ToString("X2");
							Logger.Warn("Missing __LIBS Symbol for: " + dllName + " -> " + funcName);
						}

						sb.AppendLine("    .dd " + sym);
						libs[dllName].Add(funcName, sym);
					}
				}
			}

			return sb.ToString();
		}

		public static string DisassembleDataVar(byte[] data, CodeResource code)
		{
			StringBuilder sb = new StringBuilder();

			using (MemoryStream ms = new MemoryStream(data))
			using (BinaryReader br = new BinaryReader(ms))
			{
				while (!br.Eof())
				{
					DataType type = (DataType) br.ReadInt32();
					sb.Append("    .dd 0x" + ((int) type).ToString("X2") + " ; " + type);

					if (type != DataType.End)
						sb.AppendLine();

					switch (type)
					{
						case DataType.End:
							return sb.ToString();
						case DataType.Integer:
							sb.Append("    .dd 0x" + br.ReadUInt32().ToString("X2"));
							break;
						case DataType.Float:
							sb.Append("    .dd 0x" + br.ReadSingle().ToString("X2"));
							break;
						case DataType.CString:
							uint cstringOff = br.ReadUInt32();
							string sym = code.GetSymbolName(cstringOff) ?? "0x" + cstringOff.ToString("X2");
							sb.Append("    .dd " + sym);
							break;
						default:
							uint val = br.ReadUInt32();
							sb.Append("    ; Invalid Type For __DATA: " + type + " =>" + val.ToString("X2"));
							Logger.Warn("Invalid Type For __DATA: " + type + " =>" + val.ToString("X2"));
							break;
					}

					if (type != DataType.End)
						sb.AppendLine();
				}
			}

			return sb.ToString();
		}

		public static string DisassembleVariable(string name, CodeResource code, uint offset, uint size,
			ref Dictionary<string, Dictionary<string, string>> libs)
		{
			Logger.Info("    " + offset.ToString("X8") + ": " + name);

			byte[] data = code.GetData(offset, size);

			if (IsString(name) && size > 1)
				return GetString(data);
			if (IsArray(name))
				return GetArrayString(data);
			if (name == "__LIBS")
				return DisassembleLibsVar(data, code, ref libs);
			if (name == "__DATA")
				return DisassembleDataVar(data, code);
			if (size == 1)
				return ".db 0x" + data[0].ToString("X2");
			if (size == 2)
				return ".dw 0x" + BitConverter.ToInt16(data, 0).ToString("X2");
			if (size == 4)
				return ".dd 0x" + BitConverter.ToInt32(data, 0).ToString("X2");
			return GetByteArrayString(data);
		}

		public static bool NeedsSizePrefix(Operand op)
		{
			return op.Size != 32;
		}

		public static string GetSizePrefix(int size)
		{
			switch (size)
			{
				case 1:
					return "byte ";
				case 2:
					return "word ";
				case 4:
					return "dword ";
				case 8:
					return "qword ";
				default:
					return "";
			}
		}

		public static string GetSizePrefix(Operand op)
		{
			if (!NeedsSizePrefix(op))
				return "";
			return GetSizePrefix(op.Size / 8);
		}
	}

	internal enum DataType
	{
		End = 0x00,
		Integer = 0x01,
		Float = 0x02,
		String = 0x03,
		CString = 0x04,
		Obj = 0x05,
		Vec = 0x06
	}
}