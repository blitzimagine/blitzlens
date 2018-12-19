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

		protected BlitzBasicCodeResource BBCCode;
		protected Dictionary<string, string> Variables;

		protected Dictionary<string, Dictionary<string, string>> Libs;

		private List<Instruction> _instructions;

		protected List<Instruction> Instructions
		{
			get
			{
				if (_instructions == null)
				{
					GetInstructions();
				}
				return _instructions;
			}
		}

		protected bool _commentOriginal;
		protected bool _applySymbols;

		private readonly Dictionary<uint, string> _disassembly;

		public BlitzModule(BlitzDecompiler decompiler, BlitzBasicCodeResource bbcCode, bool applySymbols = true, bool commentOriginal = false)
		{
			Variables = new Dictionary<string, string>();
			Libs = new Dictionary<string, Dictionary<string, string>>();
			_disassembly = new Dictionary<uint, string>();

			Decompiler = decompiler;
			BBCCode = bbcCode;
			_commentOriginal = commentOriginal;
			_applySymbols = applySymbols;
		}

		private void GetInstructions(uint address = 0, bool commentOriginal = false, bool applyRelocs = true)
		{
			Disassembler disasm = new Disassembler(
				BBCCode?.GetRelocatedCode(),
				ArchitectureMode.x86_32,
				address, false, Vendor.Any, address
			);

			_instructions = disasm.Disassemble().ToList();
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

		public void Disassemble()
		{
			_disassembly.Clear();

			foreach (var inst in Instructions)
			{
				if (inst.Mnemonic == ud_mnemonic_code.UD_Inop)
					continue;

				uint offset = (uint)inst.Offset;

				if (BBCCode.GetOrderedVarSymbols().Length > 0 && BBCCode.ContainsSymbol(BBCCode.GetOrderedVarSymbols()[0]) && offset >= BBCCode.GetSymbol(BBCCode.GetOrderedVarSymbols()[0]))
					break;

				string symbolName = BBCCode.GetSymbolName(offset);
				if (symbolName != null)
					Logger.Info("    " + offset.ToString("X8") + ": " + symbolName);

				if (inst.Error)
				{
					Logger.Error("<ERROR:" + offset + ">" + inst.ErrorMessage + ">");
					break;
				}

				string instStr = GetInstructionString(this, inst, _commentOriginal, _applySymbols);

				_disassembly.Add(offset, instStr);
			}
		}

		public static string GetInstructionString(BlitzModule module, Instruction instruction, bool commentOriginal = false, bool applySymbols = true)
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
				// SharpDisasm's imul instruction has 3 operands but the blitz assembler only expects the last two.
				if (instName == "imul" && i == 0)
					i++;

				string opText = operands[i].Trim();

				if (applySymbols)
					opText = ApplyRelocToOperand(module, instruction, i, opText);

				sb.Append(Utils.GetSizePrefix(instruction.Operands[i]));
				sb.Append(opText);

				if (i < instruction.Operands.Length - 1)
					sb.Append(", ");
			}

			if (commentOriginal)
				sb.Append(" ; " + instString);

			return sb.ToString();
		}

		private static string ApplyRelocToOperand(BlitzModule module, Instruction inst, int operandIndex, string originalLine)
		{
			string newLine = originalLine;

			uint off = (uint)inst.Offset;
			uint sz = (uint)inst.Length;

			Operand operand = inst.Operands[operandIndex];

			for (int i = 0; i < sz; i++)
			{
				uint newOff = (uint)i + off;

				if (!module.GetCode().HasAbsReloc(newOff))
					continue;
				string sym = module.GetCode().GetAbsRelocSymbol(newOff);

				string originalValue = "0x" + operand.Value.ToString("X").ToLower();

				uint symOff = 0;
				if (module.GetCode().ContainsSymbol(sym))
					symOff = module.GetCode().GetSymbol(sym);
				else if (module.GetCode().ContainsImport(sym))
					symOff = module.GetCode().GetImport(sym);
				if (symOff == 0)
					throw new InvalidDataException("No Symbol? (ABS) " + off.ToString("X8"));
				if (operand.Value != symOff)
					continue;
				newLine = newLine.Replace(originalValue, sym);
				return newLine;
			}

			for (int i = 0; i < sz; i++)
			{
				uint newOff = (uint)i + off;

				if (!module.GetCode().HasRelReloc(newOff))
					continue;
				string sym = module.GetCode().GetRelRelocSymbol(newOff);

				uint oVal = (uint)(off + sz + operand.Value);
				string originalValue = "0x" + oVal.ToString("X").ToLower();

				uint symOff = 0;
				if (module.GetCode().ContainsSymbol(sym))
					symOff = module.GetCode().GetSymbol(sym);
				else if (module.GetCode().ContainsImport(sym))
					symOff = module.GetCode().GetImport(sym);
				if (symOff == 0)
					throw new InvalidDataException("No Symbol? (REL) " + off.ToString("X8"));
				if (operand.Value != symOff)
					continue;
				newLine = newLine.Replace(originalValue, sym);
				return newLine;
			}

			return newLine;
		}

		public BlitzBasicCodeResource GetCode()
		{
			return BBCCode;
		}

		public Dictionary<string, string> GetVariables()
		{
			return Variables;
		}

		public Dictionary<uint, string> GetDisassembly()
		{
			return _disassembly;
		}
	}
}
