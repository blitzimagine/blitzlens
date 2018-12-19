using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
			List<uint> offsets = Disassembler.GetDisassembly().Keys.ToList();
			List<string> instructions = Disassembler.GetDisassembly().Values.ToList();

			string currentFile = null;
			string currentFunction = null;
			string currentLabel = null;

			StringBuilder sb = new StringBuilder();

			try
			{
				int count = Disassembler.GetDisassembly().Count;
				for (int i = 0; i < count; i++)
				{
					uint offset = offsets[i];
					string instruction = instructions[i];

					string label = Disassembler.GetCode().GetSymbolName(offset);
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
						currentLabel = "ERROR_LABEL_" + offset;
					if (currentFunction == null)
						currentFunction = "ERROR_FUNC_" + offset;

					if (currentLabel != "__MAIN")
					{
						string[] split = instruction.Split(' ');

						// TODO: Decompile back to BlitzBasic
						if (split[0] == "call")
						{
							if (split[1] == "__bbStrConst")
							{
								//string lastInstruction = instructions[i - 1];
								//string reg = lastInstruction.Split(',')[1].Trim();
								string inst = instructions[i - 2];
								if (inst.Trim().StartsWith("mov [esp],"))
									inst = instructions[i - 3];
								string symbol = inst.Split(',')[1].Trim();
								if (!Disassembler.GetVariables().ContainsKey(symbol))
								{
									Logger.Warn("__bbStrConst: Missing Symbol '" + symbol + "' for " + instructions[i - 2]);
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
							} else if (split[1] == "__bbDebugStmt")
							{
								string inst = instructions[i - 1];
								string symbol = inst.Split(',')[1].Trim();
								if (!Disassembler.GetVariables().ContainsKey(symbol))
								{
									Logger.Warn("__bbDebugStmt: Missing Symbol '" + symbol + "' for " + instructions[i - 2]);
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
						}
					}

					string args = "";
					string funcDecl = "Function " + currentFunction + "(" + args + ")";
					string endFunc = "End Function";
					string funcText = funcDecl + "\r\n" + sb.ToString().Trim().Indent() + "\r\n" + endFunc;

					if (i + 1 < count)
					{
						string next = Disassembler.GetCode().GetSymbolName(offsets[i + 1]);
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
