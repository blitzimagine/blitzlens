using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlitzLensLib.Decompilers.Handlers;
using BlitzLensLib.Structures;
using BlitzLensLib.Utils;

namespace BlitzLensLib.Decompilers
{
	public class BlitzDecompiler
	{
		protected BlitzLens BlitzLens;
		protected BlitzDisassembler Disassembler;

		protected Dictionary<string, string> DecompiledCode;
		protected Dictionary<string, string> FunctionFileMap;
		protected List<string> FileNames;

		public BlitzDecompiler(BlitzLens lens, BlitzDisassembler disassembler)
		{
			DecompiledCode = new Dictionary<string, string>();
			FunctionFileMap = new Dictionary<string, string>();
			FileNames = new List<string>();

			BlitzLens = lens;
			Disassembler = disassembler;
		}

		public void Decompile()
		{
			InstructionTokenizer tokenizer = new InstructionTokenizer(Disassembler.GetDisassembly());

			StringBuilder sb = new StringBuilder();

			string currentFile = null;
			string currentFunction = null;
			string currentLabel = null;
			
			try
			{
				while (tokenizer.HasNext())
				{
					ASMInstruction inst = tokenizer.NextInstruction();

					string label = Disassembler.GetCode().GetSymbolName(inst.Offset);
					if (label != null)
					{
						currentLabel = label;
						if (label.StartsWith("_f") || label == "__MAIN")
						{
							if (label.StartsWith("_f") && label.Length > 2)
								label = label.Substring(2);
							currentFunction = label;
							Logger.Info(currentFunction.Indent());
						}
					}

					if (currentLabel == null)
						currentLabel = "ERROR_LABEL_" + inst.Offset;
					if (currentFunction == null)
						currentFunction = "ERROR_FUNC_" + inst.Offset;

					if (currentLabel != "__MAIN")
						Decompile(tokenizer, sb, ref currentFile, ref currentLabel, ref currentFunction);

					string args = "";
					string funcDecl = "Function " + currentFunction + "(" + args + ")";
					string endFunc = "End Function";
					string funcText = funcDecl + "\r\n" + sb.ToString().Trim().Indent() + "\r\n" + endFunc;

					if (tokenizer.HasNext())
					{
						string next = Disassembler.GetCode().GetSymbolName(tokenizer.GetInstruction(+1).Offset);
						if (next != null && next.StartsWith("_f"))
						{
							DecompiledCode.Add(currentFunction, funcText);
							if (currentFile != null)
								FunctionFileMap.Add(currentFunction, currentFile);
							sb.Clear();
						}
					}
					else
					{
						DecompiledCode.Add(currentFunction, funcText);
						if (currentFile != null)
							FunctionFileMap.Add(currentFunction, currentFile);
						sb.Clear();
					}
				}
			}
			catch (IndexOutOfRangeException ex)
			{
				Logger.Error(ex.Message + ": " + ex.StackTrace);
			}
		}

		public void Decompile(InstructionTokenizer tokenizer, StringBuilder sb, ref string currentFile, ref string currentLabel, ref string currentFunction)
		{
			ASMInstruction instruction = tokenizer.GetInstruction();

			string[] split = instruction.Code.Split(' ');

			// TODO: Decompile back to BlitzBasic
			switch (split[0])
			{
				case "call":
					DecompileCall(tokenizer, sb, ref currentFile, ref currentLabel, ref currentFunction);
					break;
			}
		}

		public void DecompileCall(InstructionTokenizer tokenizer, StringBuilder sb, ref string currentFile, ref string currentLabel, ref string currentFunction)
		{
			ASMInstruction instruction = tokenizer.GetInstruction();

			string location = instruction.Code.Split(' ')[1];

			if (location.StartsWith("__bb"))
			{
				if (location == "__bbStrConst")
				{
					string inst = tokenizer.GetInstruction(-2).Code;
					if (inst.Trim().StartsWith("mov [esp],"))
						inst = tokenizer.GetInstruction(-3).Code;
					string symbol = inst.Split(',')[1].Trim();
					if (!Disassembler.GetVariables().ContainsKey(symbol))
					{
						Logger.Warn("__bbStrConst: Missing Symbol '" + symbol + "' for " + inst);
						sb.AppendLine(";" + inst);
					}
					else
					{
						string var = Disassembler.GetVariables()[symbol];
						if (var == ".db 0x00")
						{
							var = "\"\"";

							sb.AppendLine(var);
						}
						else
						{
							var = var.Substring(4);
							var = var.Substring(0, var.LastIndexOf(",", StringComparison.Ordinal));

							sb.AppendLine(var);
						}
					}
				}
				else if (location == "__bbDebugStmt")
				{
					string inst = tokenizer.GetInstruction(-1).Code;
					string symbol = inst.Split(',')[1].Trim();
					if (!Disassembler.GetVariables().ContainsKey(symbol))
					{
						Logger.Warn("__bbDebugStmt: Missing Symbol '" + symbol + "' for " + inst);
						sb.AppendLine("; Missing Symbol: " + inst);
					}
					else
					{
						string var = Disassembler.GetVariables()[symbol];
						var = var.Substring(4);
						var = var.Substring(0, var.LastIndexOf(",", StringComparison.Ordinal)).Trim();
						var = var.Substring(1, var.Length - 2);

						var = Path.GetFileName(var);

						currentFile = var;
						if (!FileNames.Contains(currentFile))
							FileNames.Add(currentFile);
					}
				}
				else if (location == "__bbDebugEnter")
				{

				}
				else if (location == "__bbDimArray")
				{

				}
				else if (location == "__bbUndimArray")
				{

				}
				else
				{

				}
			}
			else if (Disassembler.GetCode().ContainsSymbol(location))
			{
				// TODO: Function Args
				string args = "";
				sb.AppendLine(location.Substring(2) + "(" + args + ")");
			}
			else
			{
				// TODO: Function Args

				string args = "";
				string loc = location;
				if (loc.StartsWith("_f"))
					loc = loc.Substring(2);
				sb.AppendLine(loc + "(" + args + ")");
			}
		}

		public Dictionary<string, string> GetDecompiledCode()
		{
			return DecompiledCode;
		}

		public Dictionary<string, string> GetFunctionFileMap()
		{
			return FunctionFileMap;
		}

		public List<string> GetFileNames()
		{
			return FileNames;
		}
	}
}
