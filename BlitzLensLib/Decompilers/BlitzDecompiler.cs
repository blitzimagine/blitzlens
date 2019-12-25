using System;
using System.Collections.Generic;
using System.Globalization;
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

        private List<string> _statements;
        protected List<string> Statements
        {
            get {
                if(_statements == null) {
                    _statements = GetStatements();
                }
                return _statements;
            }
        }

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

        public List<string> GetStatements() {
            List<string> statements = new List<string>();
            statements.Add("sub");
            statements.Add("call");
            statements.Add("mov");

            return statements;
        }

		public void Decompile(InstructionTokenizer tokenizer, StringBuilder sb, ref string currentFile,
			ref string currentLabel, ref string currentFunction)
		{
			ASMInstruction instruction = tokenizer.GetInstruction();

			string opcode;

            // TODO: add "environment" to keep track of variable types
            
			for (int i=0;i<Statements.Count;i++)
			{
				if (instruction == null)
				{
                    break;
				}

				opcode = instruction.Code.Split(' ')[0];

				if (Statements[i] == opcode)
				{
					// right opcode
					switch (i) {
                        case 0:
                            DecompileSub(tokenizer, sb, ref currentFile, ref currentLabel,
                                ref currentFunction); // sub
                            break;
                        case 1:
                            DecompileCall(tokenizer, sb, ref currentFile, ref currentLabel,
                                ref currentFunction); // call
                            break;
                        case 2:
                            DecompileMov(tokenizer, sb, ref currentFile, ref currentLabel,
								ref currentFunction); // mov
							break;
					}
                }

                instruction = tokenizer.NextInstruction();
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

        public bool DecompileSub(InstructionTokenizer tokenizer, StringBuilder sb, ref string currentFile,
            ref string currentLabel, ref string currentFunction) {
            ASMInstruction instruction = tokenizer.GetInstruction();
            if (GetOperand(instruction, 0) == "esp") {
                // subtracting from stack, call with arguments
                if (!TryHarderParse(GetOperand(instruction, 1), out int numArguments)) {
                    return false;
                }
                numArguments /= 4;
                int currentArgument = 0;

                // recursively decompile while not found mov [esp], X...
                do {
                    instruction = tokenizer.NextInstruction();
                    Decompile(tokenizer, sb, ref currentFile, ref currentLabel, ref currentFunction);
                    instruction = tokenizer.GetInstruction();
                    // TODO: argumentX = ...
                    if (IsEffectiveAddress(instruction, 0) && !TryHarderParse(GetEffectiveAddress(instruction, 0, 1), out currentArgument)) {
                        return false;
                    }
                } while (currentArgument > 0);

                // now we are at the call, get function name
                instruction = tokenizer.NextInstruction();
                string functionName = GetOperand(instruction, 0);
                // TODO: is it _bbStrConst/Load/Store?
                sb.Append(functionName + "(");
                for(int i=0;i<numArguments;i++) {
                    sb.Append("a" + i);
                    if (i < numArguments) {
                        sb.Append(", ");
                    }
                }
                sb.AppendLine(")");
                return true;
            } else {
                // TODO: subtracting from a variable
                return true;
            }
            return false;
        }

        public bool DecompileCall(InstructionTokenizer tokenizer, StringBuilder sb, ref string currentFile,
            ref string currentLabel, ref string currentFunction) {
            ASMInstruction instruction = tokenizer.GetInstruction();
            string functionName = GetOperand(instruction, 0);
            sb.AppendLine(functionName + "()");
            return true;
        }

        public bool DecompileMov(InstructionTokenizer tokenizer, StringBuilder sb, ref string currentFile,
            ref string currentLabel, ref string currentFunction) {
            ASMInstruction instruction = tokenizer.GetInstruction();
            if (IsEffectiveAddress(instruction, 0) && GetEffectiveAddress(instruction, 0, 0) == "ebp") {
                if (!TryHarderParse(GetEffectiveAddress(instruction, 0, 1), out int numVariable)) {
                    return false;
                }
                numVariable /= 4;
                // TODO: but what if it's a register...
                sb.AppendLine("v" + numVariable + " = " + GetOperand(instruction, 1));
                return true;
            } else if (IsEffectiveAddress(instruction, 0) && GetEffectiveAddress(instruction, 0, 0) == "esp") {
                if (!TryHarderParse(GetEffectiveAddress(instruction, 0, 1), out int numArgument)) {
                    return false;
                }
                numArgument /= 4;
                // TODO: but what if it's a register...
                sb.AppendLine("a" + numArgument + " = " + GetOperand(instruction, 1));
                return true;
            }
            return false;
        }

        public string GetOpcode(ASMInstruction instruction) {
            return instruction.Code.Split(' ')[0].Trim();
        }

        public string GetOperand(ASMInstruction instruction, int index) {
            string result = instruction.Code.Substring(instruction.Code.IndexOf(' ')).Split(',')[index].Trim();
            if (result == "byte" || result == "word" || result == "dword") {
                result = instruction.Code.Substring(instruction.Code.IndexOf(' ')).Split(',')[index + 1].Trim();
            }
            return result;
        }

        public bool IsEffectiveAddress(ASMInstruction instruction, int operandIndex) {
            string operand = GetOperand(instruction, operandIndex);
            return (operand.Length > 3 && operand.IndexOf("[") == 0 && operand.LastIndexOf("]") == operand.Length - 1);
        }

        public string GetEffectiveAddress(ASMInstruction instruction, int operandIndex, int effectiveAddressIndex) {
            string operand = GetOperand(instruction, operandIndex);
            List<string> effectiveAddresses = new List<string>();

            effectiveAddresses.Add(operand.Substring(1, 3));

            if (operand.Length > 4) {
                effectiveAddresses.Add(operand.Substring(4, operand.Length - 5));
            } else {
                effectiveAddresses.Add("0");
            }

            return effectiveAddresses[effectiveAddressIndex].Trim();
        }

        public bool TryHarderParse(string s, out int result) {
            result = 0;
            if (string.IsNullOrEmpty(s)) {
                return false;
            }

            int state = 0;
            if (s[0] == '-') {
                state = 1;
            } else if (s[0] == '+') {
                state = 2;
            }

            if (state > 0) {
                s = s.Substring(1);
            }

            if (s.IndexOf("0x") == 0) {
                s = s.Substring(2);
            }

            if (!int.TryParse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out result)) {
                return false;
            }

            if (state == 1) {
                result *= -1;
            }
            return true;
        }

        //public void DecompileCall(InstructionTokenizer tokenizer, StringBuilder sb, ref string currentFile,
        //ref string currentLabel, ref string currentFunction) {
        /*
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
            else if (location == "__bbDebugEnter") { }
            else if (location == "__bbDimArray") { }
            else if (location == "__bbUndimArray") { }
            else { }
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
        */
        //}

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