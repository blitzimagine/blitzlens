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
		protected BlitzDecompiler Decompiler;

		protected byte[] RawCode;
		protected Dictionary<string, uint> Symbols;
		protected Dictionary<uint, string> RelativeRelocs;
		protected Dictionary<uint, string> AbsoluteRelocs;

		protected Dictionary<string, uint> Imports;

		protected byte[] RelocatedCode;

		public uint CodeSize => (uint)RelocatedCode.Length;

		private BlitzBasicCodeFile(BlitzDecompiler decompiler)
		{
			Symbols = new Dictionary<string, uint>();
			RelativeRelocs = new Dictionary<uint, string>();
			AbsoluteRelocs = new Dictionary<uint, string>();
			Imports = new Dictionary<string, uint>();

			Decompiler = decompiler;
		}

		public static BlitzBasicCodeFile FromBytes(BlitzDecompiler decompiler, byte[] bytes)
		{
			BlitzBasicCodeFile result = new BlitzBasicCodeFile(decompiler);
			
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
					symOffset = GetOrCreateImport(sym);
				
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
					symOffset = GetOrCreateImport(sym);

				RelocatedCode.SetUInt32((int)offset, symOffset);
			}
		}

		private uint GetOrCreateImport(string symbol)
		{
			if (Imports.ContainsKey(symbol))
				return Imports[symbol];

			uint baseAddr = 0x10000000;
			uint symAddr = baseAddr + ((uint) Imports.Count + 1) * 4;
			Imports.Add(symbol, symAddr);

			return symAddr;
		}

		public bool ContainsImport(string symbol)
		{
			return Imports.ContainsKey(symbol);
		}

		public uint GetImport(string symbol)
		{
			return Imports[symbol];
		}

		public string[] GetImports()
		{
			return Imports.Keys.ToArray();
		}

		public string GetImportName(uint address)
		{
			foreach (var pair in Imports)
			{
				if (pair.Value == address)
					return pair.Key;
			}

			return null;
		}

		public bool ContainsSymbol(string symbol)
		{
			return Symbols.ContainsKey(symbol);
		}

		public uint GetSymbol(string symbol)
		{
			return Symbols[symbol];
		}

		public string[] GetSymbols()
		{
			return Symbols.Keys.ToArray();
		}

		public string GetSymbolName(uint address)
		{
			foreach (var pair in Symbols)
			{
				if (pair.Value == address)
					return pair.Key;
			}

			return null;
		}

		public string GetAnyName(uint address)
		{
			if (Symbols.ContainsValue(address))
				return GetSymbolName(address);
			else if (Imports.ContainsValue(address))
				return GetImportName(address);

			return null;
		}

		public string GetRelRelocSymbol(uint address)
		{
			if (RelativeRelocs.ContainsKey(address))
				return RelativeRelocs[address];

			return null;
		}

		public string GetAbsRelocSymbol(uint address)
		{
			if (AbsoluteRelocs.ContainsKey(address))
				return AbsoluteRelocs[address];

			return null;
		}

		public Dictionary<uint, string> GetAbsRelocs()
		{
			return AbsoluteRelocs;
		}

		public Dictionary<uint, string> GetRelRelocs()
		{
			return RelativeRelocs;
		}

		public string[] GetOrderedVarSymbols()
		{
			List<Symbol> syms = new List<Symbol>();

			foreach (var pair in AbsoluteRelocs)
			{
				string symbol = pair.Value;
				bool skip = false;
				foreach (var s in syms)
				{
					if (s.Name == symbol)
					{
						skip = true;
						break;
					}
				}

				if (skip)
					continue;
				
				// when creating an import here, maybe separate it since it's a var?
				uint addr = Symbols.ContainsKey(symbol) ? Symbols[symbol] : GetOrCreateImport(symbol);

				Symbol sym = new Symbol(symbol, addr);
				syms.Add(sym);
			}

			syms.Sort((x, y) => x.Address.CompareTo(y.Address));

			List<string> result = new List<string>();
			foreach (Symbol sym in syms)
			{
				result.Add(sym.Name);
			}
			return result.ToArray();
		}

		public byte[] GetRelocatedCode()
		{
			return RelocatedCode;
		}

		public byte[] GetRawCode()
		{
			return RawCode;
		}

		public byte[] GetData(uint offset, uint size)
		{
			byte[] data = new byte[size];
			byte[] relocatedCode = GetRelocatedCode();
			for (uint i = 0; i < size; i++)
			{
				data[i] = relocatedCode[offset + i];
			}

			return data;
		}

		private class Symbol
		{
			public readonly string Name;
			public readonly uint Address;

			public Symbol(string name, uint address)
			{
				Name = name;
				Address = address;
			}
		}
	}
}
