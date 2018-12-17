using System;
using System.Collections.Generic;
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

		public BlitzModule(BlitzBasicCodeFile bbcCode)
		{
			BBCCode = bbcCode;
		}

		public string DisassembleFunction(string symbol)
		{
			if (!BBCCode.ContainsSymbol(symbol))
				return null;
			return DisassembleFunction(BBCCode.GetSymbol(symbol));
		}

		public string DisassembleFunction(uint address)
		{
			Disassembler disasm = new Disassembler(BBCCode?.GetRelocatedCode(),
												   ArchitectureMode.x86_32,
				address, false, Vendor.Any, address);
			StringBuilder sb = new StringBuilder();
			//sb.AppendLine("_off_" + address.ToString("X8") + ":");
			string symbolName = BBCCode?.GetSymbolName(address);
			if (symbolName == null)
				symbolName = "_off_" + address.ToString("X8");
			sb.AppendLine(symbolName + ":");

			while (true)
			{
				Instruction inst = disasm.NextInstruction();
				sb.AppendLine("    " + GetInstructionWithSymbols(inst));

				if (inst.Error)
				{
					sb.AppendLine("<ERROR:" + inst.ErrorMessage + ">");
					break;
				}

				if (inst.Mnemonic == ud_mnemonic_code.UD_Ijmp)
				{
					uint offset = (uint) inst.Operands[0].Value;
					string func = DisassembleFunction(offset);
					sb.AppendLine(func);

					break;
				}

				if (inst.Mnemonic == ud_mnemonic_code.UD_Iret)
					break;
			}

			return sb.ToString();
		}

		private string GetInstructionWithSymbols(Instruction instruction)
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
				string opText = GetOperandText(opOffsets[i], op) ?? operands[i].Trim();
				sb.Append(opText);
				
				if (i < instruction.Operands.Length - 1)
					sb.Append(", ");
			}

			sb.Append(" ; " + instString);

			return sb.ToString();
		}

		private string GetOperandText(uint offset, Operand operand)
		{
			// TODO: Handle operands where there are multiple words such as "dword symbolname"
			return BBCCode.GetRelocSymbol(offset);
		}
	}
}
