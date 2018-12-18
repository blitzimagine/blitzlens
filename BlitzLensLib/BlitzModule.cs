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
				Variables.Add(pair.Key, Utils.DisassembleVariable(sym, BBCCode, offset, size, ref Libs));
			}
		}

		public string DisassembleFunction(string symbol, ref Dictionary<string, string> doneFuncs, bool commentOriginal)
		{
			if (!BBCCode.ContainsSymbol(symbol))
				return null;
			if (!KnownFunctions.Contains(symbol))
				KnownFunctions.Add(symbol);
			return DisassembleFunction(BBCCode.GetSymbol(symbol), ref doneFuncs, commentOriginal);
		}

		public string DisassembleFunction(uint address, ref Dictionary<string, string> doneFuncs, bool commentOriginal)
		{
			Disassembler disasm = new Disassembler(BBCCode?.GetRelocatedCode(),
												   ArchitectureMode.x86_32,
				address, false, Vendor.Any, address);
			StringBuilder sb = new StringBuilder();
			string symbolName = BBCCode?.GetSymbolName(address);
			if (symbolName == null)
				symbolName = "_off_" + address.ToString("X8");
			sb.AppendLine(symbolName + ":");

			Logger.Info(symbolName);

			while (true)
			{
				Instruction inst = disasm.NextInstruction();
				string instStr = GetInstructionWithSymbols(inst);
				sb.Append("    " + instStr);
				if (commentOriginal)
					sb.Append(" ; " + inst);
				sb.AppendLine();

				if (inst.Error)
				{
					sb.AppendLine("<ERROR:" + inst.ErrorMessage + ">");
					break;
				}

				if (inst.Mnemonic == ud_mnemonic_code.UD_Icall ||
				    inst.Mnemonic == ud_mnemonic_code.UD_Ijmp ||
				    inst.Mnemonic == ud_mnemonic_code.UD_Ijz ||
				    inst.Mnemonic == ud_mnemonic_code.UD_Ijnz ||
				    inst.Mnemonic == ud_mnemonic_code.UD_Ijg ||
					inst.Mnemonic == ud_mnemonic_code.UD_Ijge ||
				    inst.Mnemonic == ud_mnemonic_code.UD_Ijl ||
					inst.Mnemonic == ud_mnemonic_code.UD_Ijle ||
				    inst.Mnemonic == ud_mnemonic_code.UD_Ija ||
				    inst.Mnemonic == ud_mnemonic_code.UD_Ijae ||
				    inst.Mnemonic == ud_mnemonic_code.UD_Ijb ||
				    inst.Mnemonic == ud_mnemonic_code.UD_Ijbe ||
				    inst.Mnemonic == ud_mnemonic_code.UD_Ijcxz ||
				    inst.Mnemonic == ud_mnemonic_code.UD_Ijecxz ||
				    inst.Mnemonic == ud_mnemonic_code.UD_Ijo ||
				    inst.Mnemonic == ud_mnemonic_code.UD_Ijno ||
				    inst.Mnemonic == ud_mnemonic_code.UD_Ijp ||
				    inst.Mnemonic == ud_mnemonic_code.UD_Ijnp ||
				    inst.Mnemonic == ud_mnemonic_code.UD_Ijs ||
				    inst.Mnemonic == ud_mnemonic_code.UD_Ijns)
				{
					uint off = (uint)inst.Operands[0].Value;
					string funcName = BBCCode.GetSymbolName(off);
					if (funcName != null && !KnownFunctions.Contains(funcName) && BBCCode.ContainsSymbol(funcName))
						KnownFunctions.Add(funcName);
				}
				
				string symbolName2 = BBCCode?.GetSymbolName((uint)(inst.Offset + (uint)inst.Length));
				if (symbolName2 != null && symbolName2 != symbolName)
				{
					if (!KnownFunctions.Contains(symbolName2))
						KnownFunctions.Add(symbolName2);
					if (!doneFuncs.ContainsKey(symbolName2))
						doneFuncs.Add(symbolName2, DisassembleFunction(symbolName2, ref doneFuncs, commentOriginal));
					break;
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

			for (int i = 0; i < instruction.Operands.Length; i++)
			{
				if (instName == "imul" && i == 0)
					i++;

				Operand op = instruction.Operands[i];
				string opText = GetOperandText(instruction, operands[i].Trim(), op) ?? operands[i].Trim();
				
				sb.Append(Utils.GetSizePrefix(op.Size));
				sb.Append(opText);
				
				if (i < instruction.Operands.Length - 1)
					sb.Append(", ");
			}

			if (commentOriginal)
				sb.Append(" ; " + instString);

			return sb.ToString();
		}

		private string GetOperandText(Instruction inst, string originalLine, Operand operand)
		{
			string newLine = originalLine;

			uint off = (uint) inst.Offset;
			uint sz = (uint) inst.Length;
			
			foreach (var absReloc in BBCCode.GetAbsRelocs())
			{
				var absOff = absReloc.Key;
				if (absOff <= off || absOff >= off + sz)
					continue;

				string originalValue = "0x" + operand.Value.ToString("X").ToLower();
				string sym = absReloc.Value;
				uint symOff = 0;
				if (BBCCode.ContainsSymbol(sym))
					symOff = BBCCode.GetSymbol(sym);
				else if (BBCCode.ContainsImport(sym))
					symOff = BBCCode.GetImport(sym);
				if (symOff == 0)
					throw new InvalidDataException("No Symbol? (ABS) " + off.ToString("X8"));
				if (operand.Value != symOff)
					continue;
				newLine = newLine.Replace(originalValue, sym);
				return newLine;
			}

			foreach (var relReloc in BBCCode.GetRelRelocs())
			{
				var relOff = relReloc.Key;
				if (relOff <= off || relOff >= off + sz)
					continue;

				uint oVal = (uint)(off + sz + operand.Value);
				string originalValue = "0x" + oVal.ToString("X").ToLower();
				string sym = relReloc.Value;
				uint symOff = 0;
				if (BBCCode.ContainsSymbol(sym))
					symOff = BBCCode.GetSymbol(sym);
				else if (BBCCode.ContainsImport(sym))
					symOff = BBCCode.GetImport(sym);
				if (symOff == 0)
					throw new InvalidDataException("No Symbol? (REL) " + off.ToString("X8"));
				if (operand.Value != symOff)
					continue;
				newLine = newLine.Replace(originalValue, sym);
				return newLine;
			}

			return newLine;
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
