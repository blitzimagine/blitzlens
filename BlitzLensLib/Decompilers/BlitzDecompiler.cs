using System;
using System.Collections.Generic;
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

		public BlitzDecompiler(BlitzLens lens, BlitzDisassembler disassembler)
		{
			DecompiledCode = new Dictionary<string, string>();

			BlitzLens = lens;
			Disassembler = disassembler;
		}

		public void Decompile()
		{
			List<uint> offsets = Disassembler.GetDisassembly().Keys.ToList();
			List<string> instructions = Disassembler.GetDisassembly().Values.ToList();

			string currentFunction = null;
			string currentLabel = null;

			for (int i = 0; i < Disassembler.GetDisassembly().Count; i++)
			{
				uint offset = offsets[i];
				string instruction = instructions[i];

				string label = Disassembler.GetCode().GetSymbolName(offset);
				if (label != null)
				{
					currentLabel = label;
					if (label.StartsWith("_f") || label == "__MAIN")
					{
						currentFunction = label;
						Logger.Info(currentFunction.Indent());
					}
				}

				// TODO: Decompile back to BlitzBasic
			}
		}
	}
}
