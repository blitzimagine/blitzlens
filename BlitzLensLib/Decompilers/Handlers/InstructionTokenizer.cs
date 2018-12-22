using System.Collections.Generic;
using System.Linq;
using BlitzLensLib.Structures;

namespace BlitzLensLib.Decompilers.Handlers
{
	public class InstructionTokenizer
	{
		private readonly List<uint> _offsets;
		private readonly List<string> _instructions;

		private int _index;

		public int Count => _offsets.Count;

		public InstructionTokenizer(Dictionary<uint, string> instructions)
		{
			_offsets = instructions.Keys.ToList();
			_instructions = instructions.Values.ToList();

			_index = 0;
		}

		public void SeekAddress(uint address)
		{
			for (int i = 0; i < _offsets.Count; i++)
			{
				if (_offsets[i] != address)
					continue;

				_index = i;
				break;
			}
		}

		public void SeekIndex(int index)
		{
			_index = index - 1;
		}

		public bool HasNext()
		{
			return _index + 1 < Count;
		}

		public void Next()
		{
			_index++;
		}

		public bool HasPrev()
		{
			return _index > 0;
		}

		public void Prev()
		{
			_index--;
		}

		public ASMInstruction GetInstructionAt(int addr)
		{
			if (addr < 0 || addr >= Count)
				return null;
			return new ASMInstruction(_offsets[addr], _instructions[addr]);
		}

		public ASMInstruction GetInstruction(int offset = 0)
		{
			int addr = _index + offset;
			if (addr < 0 || addr >= Count)
				return null;
			return new ASMInstruction(_offsets[addr], _instructions[addr]);
		}

		public ASMInstruction NextInstruction()
		{
			var result = GetInstruction();
			Next();
			return result;
		}
	}
}