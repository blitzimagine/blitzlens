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

		private BlitzBasicCodeFile()
		{
			Symbols = new Dictionary<string, uint>();
			RelativeRelocs = new Dictionary<uint, string>();
			AbsoluteRelocs = new Dictionary<uint, string>();
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

		public byte[] GetCode()
		{
			// TODO: Implement
			return null;
		}
	}
}
