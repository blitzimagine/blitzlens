using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlitzLensLib
{
	public class BlitzBasicCodeFile
	{
		protected byte[] RawCode;
		protected Dictionary<string, uint> Symbols;
		protected Dictionary<uint, string> RelativeRelocs;
		protected Dictionary<uint, string> AbsoluteRelocs;

		protected Dictionary<string, uint> Imports;

		protected byte[] RelocatedCode;

		private BlitzBasicCodeFile()
		{
			Symbols = new Dictionary<string, uint>();
			RelativeRelocs = new Dictionary<uint, string>();
			AbsoluteRelocs = new Dictionary<uint, string>();
			Imports = new Dictionary<string, uint>();
		}

		public static BlitzBasicCodeFile FromBytes(byte[] bytes)
		{
			BlitzBasicCodeFile result = new BlitzBasicCodeFile();
			
			using (MemoryStream ms = new MemoryStream(bytes))
			using (BinaryReader br = new BinaryReader(ms))
			{
				if (!result.Read(br))
					return null;
			}

			result.ApplyRelocations();

			return result;
		}

		private bool Read(BinaryReader br)
		{
			try
			{
				int codeSize = br.ReadInt32();
				RawCode = br.ReadBytes(codeSize);

				int symbolCount = br.ReadInt32();
				for (int i = 0; i < symbolCount; i++)
				{
					Symbols.Add(br.ReadCString(), br.ReadUInt32());
				}

				int relRelocCount = br.ReadInt32();
				for (int i = 0; i < relRelocCount; i++)
				{
					string sym = br.ReadCString();
					uint offset = br.ReadUInt32();
					RelativeRelocs.Add(offset, sym);
				}

				int absRelocCount = br.ReadInt32();
				for (int i = 0; i < absRelocCount; i++)
				{
					string sym = br.ReadCString();
					uint offset = br.ReadUInt32();
					AbsoluteRelocs.Add(offset, sym);
				}
			}
			catch (IOException)
			{
				return false;
			}

			return true;
		}

		public void ApplyRelocations()
		{
			RelocatedCode = new byte[RawCode.Length];
			Array.Copy(RawCode, RelocatedCode, RawCode.Length);

			foreach (var pair in RelativeRelocs)
			{
				uint offset = pair.Key;
				string sym = pair.Value;
				uint symOffset;
				if (Symbols.ContainsKey(sym))
					symOffset = Symbols[sym];
				else
					symOffset = GetImport(sym);
				
				RelocatedCode.SetUInt32((int)offset, symOffset);
			}

			foreach (var pair in AbsoluteRelocs)
			{
				uint offset = pair.Key;
				string sym = pair.Value;
				uint symOffset;
				if (Symbols.ContainsKey(sym))
					symOffset = Symbols[sym];
				else
					symOffset = GetImport(sym);

				RelocatedCode.SetUInt32((int)offset, symOffset);
			}
		}

		public uint GetImport(string symbol)
		{
			if (Imports.ContainsKey(symbol))
				return Imports[symbol];

			uint baseAddr = 0x10000000;
			uint symAddr = baseAddr + ((uint) Imports.Count + 1) * 4;
			Imports.Add(symbol, symAddr);

			return symAddr;
		}

		public byte[] GetRelocatedCode()
		{
			return RelocatedCode;
		}

		public byte[] GetRawCode()
		{
			return RawCode;
		}
	}
}
