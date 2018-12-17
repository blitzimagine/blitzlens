using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlitzLensLib
{
	public class BlitzDecompiler
	{
		public delegate void OnLogged(LogLevel level, string msg);
		public event OnLogged Logged;

		public delegate void OnTaskSet(string task);
		public event OnTaskSet TaskSet;

		public delegate void OnHeaderSet(string header);
		public event OnHeaderSet HeaderSet;

		public delegate void OnExited(string msg, int exitCode = 0);
		public event OnExited Exited;

		protected Logger Logger;
		protected string Task;
		protected string Header;

		protected string InputPath;
		protected string OutputPath;

		private Dictionary<string, string> _disassembledFunctions;

		public BlitzDecompiler(string inputPath, string outputPath)
		{
			_disassembledFunctions = new Dictionary<string, string>();

			Logger = new Logger();
			Logger.Logged += (level, msg) => Logged?.Invoke(level, msg);

			InputPath = inputPath;
			OutputPath = outputPath;
		}

		internal void SetTask(string task)
		{
			if (Task == task)
				return;
			Task = task;
			TaskSet?.Invoke(task);
		}

		internal void SetHeader(string header)
		{
			if (Header == header)
				return;
			Header = header;
			HeaderSet?.Invoke(header);
		}

		internal void Exit(string msg, int exitCode = 0)
		{
			Exited?.Invoke(msg, exitCode);
		}

		internal void Exit(int exitCode = 0)
		{
			Exit(null, exitCode);
		}

		public void Process()
		{
			SetHeader("Initializing");

			SetTask("Extracting bbc file from \"" + Path.GetFileName(InputPath) + "\"");

			byte[] resource = BlitzUtils.GetBlitzCodeFromExecutable(InputPath);
			if (resource == null)
				Exit("Failed to extract bbc file", -2);

			SetTask("Parsing bbc file");
			BlitzBasicCodeFile bbcCode = BlitzBasicCodeFile.FromBytes(resource);
			if (bbcCode == null)
				Exit("Failed to parse bbc file", -3);

			BlitzModule module = new BlitzModule(bbcCode);
			Disassemble(module);

			Directory.CreateDirectory(OutputPath);

			using (StreamWriter sw = new StreamWriter(OutputPath + "test.asm"))
			{
				foreach (var func in module.GetKnownFunctions())
				{
					sw.WriteLine(_disassembledFunctions[func]);
					string f = func;
					//if (_disassembledFunctions.ContainsKey(func))
					//	f += " - Done";
					Logger.Debug(f);


				}
			}

			Exit("Done");
		}

		private void Disassemble(BlitzModule module)
		{
			_disassembledFunctions.Add("__MAIN", module.DisassembleFunction("__MAIN"));

			while (true)
			{
				int oldLen = module.GetKnownFunctions().Length;
				foreach (var func in module.GetKnownFunctions())
				{
					if (!_disassembledFunctions.ContainsKey(func))
					{

						_disassembledFunctions.Add(func, module.DisassembleFunction(func));


						//Logger.Warn(_disassembledFunctions[func]);
					}
				}

				if (module.GetKnownFunctions().Length <= oldLen)
					break;
			}
		}
	}
}
