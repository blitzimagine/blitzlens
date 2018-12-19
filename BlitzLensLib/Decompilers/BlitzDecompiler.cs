using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlitzLensLib.Structures;

namespace BlitzLensLib.Decompilers
{
	public class BlitzDecompiler
	{
		protected BlitzLens BlitzLens;
		protected BlitzDisassembler Disassembler;

		public BlitzDecompiler(BlitzLens lens, BlitzDisassembler disassembler)
		{
			BlitzLens = lens;
			Disassembler = disassembler;
		}

		public void Decompile()
		{
			// TODO: Decompile back to BlitzBasic
		}
	}
}
