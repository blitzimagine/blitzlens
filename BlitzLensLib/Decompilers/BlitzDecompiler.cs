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

		public void Decompile(InstructionTokenizer tokenizer, StringBuilder sb, ref string currentFile, ref string currentLabel, ref string currentFunction) {
            TreeNode<string> statements = new TreeNode<string>("statements");
            TreeNode<string> statement0;
            TreeNode<string> statement1;
            TreeNode<string> statement2;
            TreeNode<string> statement3;
            TreeNode<string> statement4;
            TreeNode<string> statement5;
            TreeNode<string> statement6;
            TreeNode<string> statement7;
            TreeNode<string> statement8;
            TreeNode<string> statement9;
            TreeNode<string> statement10;
            TreeNode<string> statement11;
            TreeNode<string> statement12;
            {
                statement0 = statements.AddChild("sub");
                {
                    statement1 = statement0.AddChild("lea");
                    {
                        statement2 = statement1.AddChild("mov");
                        {
                            statement3 = statement2.AddChild("mov");
                            {
                                statement4 = statement3.AddChild("sub");
                                {
                                    statement5 = statement4.AddChild("mov");
                                    {
                                        statement6 = statement5.AddChild("mov");
                                        {
                                            statement7 = statement6.AddChild("mov");
                                            {
                                                statement8 = statement7.AddChild("call");
                                                {
                                                    statement9 = statement8.AddChild("mov");
                                                    {
                                                        statement10 = statement9.AddChild("mov");
                                                        {
                                                            statement11 = statement10.AddChild("mov");
                                                            {
                                                                statement12 = statement11.AddChild("call"); // string assignment
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                statement1 = statement0.AddChild("mov");
                {
                    statement2 = statement1.AddChild("mov");
                    {
                        statement3 = statement2.AddChild("call");
                        {
                            statement4 = statement3.AddChild("mov");
                            {
                                statement5 = statement4.AddChild("call"); // function call (TODO: different arguments!)
                            }
                        }
                    }
                }
            }
            {
                statements.AddChild("call"); // hmmm
            }
            {
                statements.AddChild("mov"); // set int/float
            }

            ASMInstruction instruction;

            string opcode;

            int branch = 0;

            bool wrongBranch = false;

            foreach (TreeNode<string> statement in statements) {
                if (wrongBranch && !statement.IsLeaf) {
                    continue;
                }

                if (wrongBranch) {
                    wrongBranch = false;
                    continue;
                }

                if (statement.Level < 1) {
                    continue;
                }

                instruction = tokenizer.GetInstruction(statement.Level - 1);

                if (instruction == null) {
                    // branch out of bounds
                    // if this is the leaf, when we continue we might be in the right branch
                    if (!statement.IsLeaf) {
                        wrongBranch = true;
                    }
                    branch++;
                    continue;
                }

                opcode = instruction.Code.Split(' ')[0];

                if (statement.Data != opcode) {
                    // wrong branch
                    // if this is the leaf, when we continue we might be in the right branch
                    if (!statement.IsLeaf) {
                        wrongBranch = true;
                    }
                    branch++;
                    continue;
                }

                if (statement.IsLeaf && statement.Data == opcode) {
                    // right branch
                    switch (branch) {
                        case 0:
                        DecompileStringAssignment(tokenizer, sb, ref currentFile, ref currentLabel, ref currentFunction);
                        break;
                        case 1:
                        DecompileFunctionCall(tokenizer, sb, ref currentFile, ref currentLabel, ref currentFunction);
                        break;
                        case 2:
                        DecompileCall(tokenizer, sb, ref currentFile, ref currentLabel, ref currentFunction);
                        break;
                        case 3:
                        DecompileSetIntFloat(tokenizer, sb, ref currentFile, ref currentLabel, ref currentFunction);
                        break;
                    }
                    for (int i = 0;i < statement.Level - 1;i++) {
                        tokenizer.Next();
                    }
                }
            }

            // TODO: Decompile back to BlitzBasic
            /*
			switch (split[0])
			{
				case "call":
					DecompileCall(tokenizer, sb, ref currentFile, ref currentLabel, ref currentFunction);
					break;
			}
            */
        }
        public void DecompileStringAssignment(InstructionTokenizer tokenizer, StringBuilder sb, ref string currentFile, ref string currentLabel, ref string currentFunction) {
            try {
                ASMInstruction instruction = tokenizer.GetInstruction(8);
                string location = instruction.Code.Split(' ')[1];

                if (location == "__bbStrConst") {
                    instruction = tokenizer.GetInstruction(12);
                    location = instruction.Code.Split(' ')[1];

                    if (location == "__bbStrStore") {
                        instruction = tokenizer.GetInstruction(1);
                        location = instruction.Code.Split('-')[1];
                        location = location.Split(']')[0];
                        // Matt is going to yell my head off later...
                        int locationDecimal = 0;
                        try {
                            locationDecimal = Convert.ToInt32(location, 16);
                        } catch (FormatException) {
                            // Fail silently.
                        } catch (OverflowException) {
                            // Fail silently.
                        }

                        ASMInstruction instruction2 = tokenizer.GetInstruction(5);
                        string location2 = instruction.Code.Split(',')[1];

                        sb.AppendLine("v" + locationDecimal + " = " + location2);
                    }
                }
            } catch (IndexOutOfRangeException) {
                // Fail silently.
            }
        }
        public void DecompileFunctionCall(InstructionTokenizer tokenizer, StringBuilder sb, ref string currentFile, ref string currentLabel, ref string currentFunction) {
            try {
                ASMInstruction instruction = tokenizer.GetInstruction(3);
                string location = instruction.Code.Split(' ')[1];

                if (location == "__bbStrFromInt") {
                    instruction = tokenizer.GetInstruction(5);
                    location = instruction.Code.Split(' ')[1];
                    ASMInstruction instruction2 = tokenizer.GetInstruction(1);
                    string location2 = instruction2.Code.Split('-')[1];
                    location2 = location2.Split(']')[0];
                    int location2Decimal = 0;
                    try {
                        location2Decimal = Convert.ToInt32(location2, 16);
                    } catch (FormatException) {
                        // Fail silently.
                    } catch (OverflowException) {
                        // Fail silently.
                    }

                    sb.AppendLine(location.Substring(2) + "(v" + location2Decimal + ")");
                }
            } catch (IndexOutOfRangeException) {
                // Fail silently.
            }
        }
        public void DecompileSetIntFloat(InstructionTokenizer tokenizer, StringBuilder sb, ref string currentFile, ref string currentLabel, ref string currentFunction) {
            try {
                ASMInstruction instruction = tokenizer.GetInstruction();
                string location = instruction.Code.Split('-')[1];
                location = location.Split(']')[0];
                string[] location2 = instruction.Code.Split(' ');
                string location3 = location2[location2.Length - 1];
                int locationDecimal = 0;
                try {
                    locationDecimal = Convert.ToInt32(location, 16);
                } catch(FormatException ex) {
                    // Fail silently.
                }
                int location3Decimal = 0;
                try {
                    location3Decimal = Convert.ToInt32(location3, 16);
                } catch (FormatException) {
                    // Fail silently.
                } catch (OverflowException) {
                    // Fail silently.
                }
                sb.AppendLine("v" + locationDecimal + " = " + location3Decimal); // TODO: floats
            } catch (IndexOutOfRangeException) {
                // Fail silently.
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
