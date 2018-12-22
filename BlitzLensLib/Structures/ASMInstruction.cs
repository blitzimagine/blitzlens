using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlitzLensLib.Structures
{
	public class ASMInstruction
	{
		public readonly uint Offset;
		public readonly string Code;

		public ASMInstruction(uint offset, string code)
		{
			Offset = offset;
			Code = code;
		}

		public override bool Equals(object obj)
		{
			if (!(obj is ASMInstruction))
			{
				return false;
			}

			var instruction = (ASMInstruction) obj;
			return this.Offset == instruction.Offset &&
			       this.Code == instruction.Code;
		}

		public override int GetHashCode()
		{
			var hashCode = 1886220174;
			hashCode = hashCode * -1521134295 + this.Offset.GetHashCode();
			hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(this.Code);
			return hashCode;
		}

		public override string ToString()
		{
			return Offset.ToString("X8") + ": " + Code;
		}
	}
}