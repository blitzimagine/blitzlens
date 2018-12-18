using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDisasm;
using SharpDisasm.Udis86;

namespace BlitzLensLib
{
	public class BlitzModule
	{
		protected BlitzDecompiler Decompiler;

		protected BlitzBasicCodeFile BBCCode;
		protected List<string> KnownFunctions;
		protected Dictionary<string, string> Variables;

		protected Dictionary<string, Dictionary<string, string>> Libs;

		public BlitzModule(BlitzDecompiler decompiler, BlitzBasicCodeFile bbcCode)
		{
			BBCCode = bbcCode;
			KnownFunctions = new List<string>();
			Variables = new Dictionary<string, string>();
			Libs = new Dictionary<string, Dictionary<string, string>>();

			Decompiler = decompiler;
		}

		public void ProcessVariables()
		{
			Dictionary<string, uint> varSizes = new Dictionary<string, uint>();

			List<string> syms = new List<string>();

			foreach (var sym in BBCCode.GetOrderedVarSymbols())
			{
				if (!BBCCode.ContainsSymbol(sym))
					continue;
				syms.Add(sym);
			}

			for (int i = 0; i < syms.Count; i++)
			{
				var sym = syms[i];

				uint offset = BBCCode.GetSymbol(sym);

				uint size;

				if (i < syms.Count - 1)
				{
					uint offsetNext = BBCCode.GetSymbol(syms[i + 1]);
					size = offsetNext - offset;
				}
				else
				{
					size = BBCCode.CodeSize - offset;
				}
				varSizes.Add(sym, size);
			}

			foreach (var pair in varSizes)
			{
				string sym = pair.Key;
				if (!BBCCode.ContainsSymbol(sym))
					continue;
				uint offset = BBCCode.GetSymbol(sym);
				uint size = pair.Value;
				Variables.Add(pair.Key, DisassembleVariable(sym, offset, size));
			}
		}

		private byte[] GetVariableData(uint offset, uint size)
		{
			byte[] data = new byte[size];
			byte[] code = BBCCode.GetRelocatedCode();
			for (uint i = 0; i < size; i++)
			{
				data[i] = code[offset + i];
			}

			return data;
		}

		private byte[] TrimNops(string name, byte[] data)
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

		/*private bool IsString(byte[] data)
		{
			data = TrimNops(data);
			if (data.Length < 4)
				return false;

			if (data[data.Length - 1] != 0x00 && data[data.Length - 1] != 0x90)  // handle aligns
				return false;

			for (int i = 0; i < data.Length - 1; i++)
			{
				byte b = data[i];
				if (b < 0x20 || b >= 0x7F || b == 0x90) // handle aligns
					return false;
			}

			return true;
		}*/

		private bool IsDigitString(string s)
		{
			foreach (char c in s)
			{
				if (!char.IsDigit(c))
					return false;
			}

			return true;
		}

		private bool IsString(string name)
		{
			// All and Only string variable names are an underscore followed by a decimal number
			if (name.Length <= 1)
				return false;
			return name.StartsWith("_") && IsDigitString(name.Substring(1));
		}

		private string GetString(byte[] data)
		{
			//data = TrimNops(data);
			StringBuilder sb = new StringBuilder();
			sb.Append(".db \"");
			for (int i = 0; i < data.Length; i++)
			{
				if (data[i] == 0)
					break;
				if (data[i] == '\\')
					sb.Append('\\');
				sb.Append((char)data[i]);
			}

			sb.Append("\", 0x00");

			return sb.ToString();
		}

		private bool IsArray(string name)
		{
			return name.StartsWith("_a") && name.Length > 2;
		}

		private string GetArrayString(byte[] data)
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

			/*for (int i = 0; i < data.Length; i++)
			{
				sb.Append("    .db 0x" + data[i].ToString("X2"));
				if (i < data.Length - 1)
					sb.AppendLine("    ");
			}*/

			return sb.ToString();
		}

		private string GetByteArrayString(byte[] data)
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

		private string DisassembleLibsVar(byte[] data)
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

					Libs.Add(dllName, new Dictionary<string, string>());

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
						string sym = BBCCode.GetSymbolName(offset);
						if (sym == null)
						{
							sym = "0x" + offset.ToString("X2");
							Decompiler.Logger.Warn("Missing __LIBS Symbol for: " + dllName + " -> " + funcName);
						}
						sb.AppendLine("    .dd " + sym);
						Libs[dllName].Add(funcName, sym);
					}

				}
			}

			return sb.ToString();
		}

		private enum DataType
		{
			End = 0x00,
			Integer = 0x01,
			Float = 0x02,
			String = 0x03,
			CString = 0x04,
			Obj = 0x05,
			Vec = 0x06,
		}

		private string DisassembleDataVar(byte[] data)
		{
			//return ".db 0 ; TODO __DATA";

			StringBuilder sb = new StringBuilder();

			using (MemoryStream ms = new MemoryStream(data))
			using (BinaryReader br = new BinaryReader(ms))
			{
				while (!br.Eof())
				{
					DataType type = (DataType) br.ReadInt32();
					sb.Append("    .dd 0x" + ((int)type).ToString("X2") + " ; " + type);
					
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
							string sym = BBCCode.GetSymbolName(cstringOff) ?? "0x" + cstringOff.ToString("X2");
							sb.Append("    .dd " + sym);
							break;
						default:
							uint val = br.ReadUInt32();
							sb.Append("    ; Invalid Type For __DATA: " + type + " =>" + val.ToString("X2"));
							Decompiler.Logger.Warn("Invalid Type For __DATA: " + type + " =>" + val.ToString("X2"));
							break;
					}

					if (type != DataType.End)
						sb.AppendLine();
				}
			}

			return sb.ToString();
		}

		private string DisassembleVariable(string name, uint offset, uint size)
		{
			byte[] data = GetVariableData(offset, size);

			if (IsString(name) && size > 1)
				return GetString(data);
			if (IsArray(name))
				return GetArrayString(data);
			if (name == "__LIBS")
				return DisassembleLibsVar(data);
			if (name == "__DATA")
				return DisassembleDataVar(data);
			else if (size == 1)
				return ".db 0x" + data[0].ToString("X2");
			//else if (IsString(name, data))
			//	return GetString(data);
			else if (size == 2)
				return ".dw 0x" + BitConverter.ToInt16(data, 0).ToString("X2");
			else if (size == 4)
				return ".dd 0x" + BitConverter.ToInt32(data, 0).ToString("X2");
			else
				return GetByteArrayString(data);
		}

		public string DisassembleFunction(string symbol)
		{
			if (!BBCCode.ContainsSymbol(symbol))
				return null;
			if (!KnownFunctions.Contains(symbol))
				KnownFunctions.Add(symbol);
			return DisassembleFunction(BBCCode.GetSymbol(symbol));
		}

		public string DisassembleFunction(uint address = 0x0)
		{
			Disassembler disasm = new Disassembler(BBCCode?.GetRelocatedCode(),
												   ArchitectureMode.x86_32,
				address, false, Vendor.Any, address);
			StringBuilder sb = new StringBuilder();
			string symbolName = BBCCode?.GetSymbolName(address);
			if (symbolName == null)
				symbolName = "_off_" + address.ToString("X8");
			sb.AppendLine(symbolName + ":");

			while (true)
			{
				Instruction inst = disasm.NextInstruction();
				//if (inst.Offset == 0xA1EF)
				//	Debugger.Break();
				string instStr = GetInstructionWithSymbols(inst);
				//sb.AppendLine("    " + instStr + " ; " + inst);

				sb.AppendLine("    " + instStr);

				if (inst.Error)
				{
					sb.AppendLine("<ERROR:" + inst.ErrorMessage + ">");
					break;
				}

				/*if (inst.Mnemonic == ud_mnemonic_code.UD_Ijmp)
				{
					uint offset = (uint) inst.Operands[0].Value;
					string func = DisassembleFunction(offset);
					sb.AppendLine(func);

					break;
				}*/

				if (inst.Mnemonic == ud_mnemonic_code.UD_Icall || inst.Mnemonic == ud_mnemonic_code.UD_Ijmp)
				{
					int index = instStr.IndexOf(' ');
					string funcName = instStr.Substring(index).Trim();
					if (!KnownFunctions.Contains(funcName) && BBCCode.ContainsSymbol(funcName))
						KnownFunctions.Add(funcName);
				}

				if (inst.Mnemonic == ud_mnemonic_code.UD_Iret || inst.Mnemonic == ud_mnemonic_code.UD_Ijmp)
					break;
			}

			return sb.ToString();
		}

		private string GetInstructionWithSymbols(Instruction instruction, bool commentOriginal = false)
		{
			StringBuilder sb = new StringBuilder();

			string instString = instruction.ToString();
			int index = instString.IndexOf(' ');
			string instName;
			if (index == -1)
				instName = instString;
			else
				instName = instString.Substring(0, index);
			sb.Append(instName + " ");

			string[] operands = new string[0];
			if (index != -1)
				operands = instString.Substring(index).Split(',');

			uint lastOffset = (uint)instruction.Offset + (uint)instruction.Length;

			uint[] opOffsets = new uint[instruction.Operands.Length];
			for (int i = opOffsets.Length - 1; i >= 0; i--)
			{
				// TODO: Get operand size correctly instead of this
				uint opSize = (uint) instruction.Operands[i].Size / 8;
				lastOffset = lastOffset - opSize;
				opOffsets[i] = lastOffset;
			}

			if (instName == "ret")
				sb.Append("word ");

			for (int i = 0; i < instruction.Operands.Length; i++)
			{
				Operand op = instruction.Operands[i];
				string opText = GetOperandText(opOffsets[i], operands[i].Trim(), op) ?? operands[i].Trim();
				if (instName == "shl" && i == 1)
					sb.Append("byte ");
				sb.Append(opText);
				
				if (i < instruction.Operands.Length - 1)
					sb.Append(", ");
			}

			if (commentOriginal)
				sb.Append(" ; " + instString);

			return sb.ToString();
		}

		private string GetOperandText(uint offset, string originalLine, Operand operand)
		{
			// This should work for all cases. HOPEFULLY!
			string from = "0x" + operand.Value.ToString("X").ToLower();
			string to = BBCCode.GetAbsRelocSymbol(offset);
			if (to == null)
			{
				uint opSize = (uint)operand.Size / 8;
				from = "0x" + (offset + operand.Value + opSize).ToString("X").ToLower();
				to = BBCCode.GetRelRelocSymbol(offset);
			}

			if (to == null)
				return null;

			return originalLine.Replace(from, to);
		}

		public string[] GetKnownFunctions()
		{
			return KnownFunctions.ToArray();
		}

		public Dictionary<string, string> GetVariables()
		{
			return Variables;
		}
	}
}
