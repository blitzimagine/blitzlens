using System;
using System.Collections.Generic;
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
		protected BlitzBasicCodeFile BBCCode;
		protected List<string> KnownFunctions;

		public BlitzModule(BlitzBasicCodeFile bbcCode)
		{
			BBCCode = bbcCode;
			KnownFunctions = new List<string>();
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
				string instStr = GetInstructionWithSymbols(inst);
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
			if (index == -1)
				sb.Append(instString + " ");
			else
				sb.Append(instString.Substring(0, index) + " ");

			string[] operands = new string[0];
			if (index != -1)
				operands = instString.Substring(index).Split(',');

			uint lastOffset = (uint)instruction.Offset + (uint)instruction.Length;

			uint[] opOffsets = new uint[instruction.Operands.Length];
			for (int i = opOffsets.Length - 1; i >= 0; i--)
			{
				uint opSize = (uint) instruction.Operands[i].Size / 8;
				lastOffset = lastOffset - opSize;
				opOffsets[i] = lastOffset;
			}

			for (int i = 0; i < instruction.Operands.Length; i++)
			{
				Operand op = instruction.Operands[i];
				string opText = GetOperandText(opOffsets[i], operands[i].Trim(), op) ?? operands[i].Trim();
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
	}
}
