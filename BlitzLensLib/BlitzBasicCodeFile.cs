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
		private byte[] _rawCode;

		private BlitzBasicCodeFile()
		{
			
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
			// TODO: Implement
			return false;
		}

		public byte[] GetCode()
		{
			// TODO: Implement
			return null;
		}
	}
}
