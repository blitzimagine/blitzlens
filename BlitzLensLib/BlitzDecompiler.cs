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
		
		protected string Task;
		protected string Header;

		protected string InputPath;
		protected string OutputPath;

		private Dictionary<string, string> _disassembledFunctions;

		public BlitzDecompiler(string inputPath, string outputPath)
		{
			_disassembledFunctions = new Dictionary<string, string>();
			
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

			Logger.Info("Processing: \"" + Path.GetFileName(InputPath) + "\"");

			SetTask("Extracting BBC Resource");
			byte[] resource = BlitzUtils.GetBlitzCodeFromExecutable(InputPath);
			if (resource == null)
				Exit("Failed to extract BBC resource", 2);

			SetTask("Parsing BBC Resource");
			BlitzBasicCodeFile bbcCode = BlitzBasicCodeFile.FromBytes(this, resource);
			if (bbcCode == null)
				Exit("Failed to parse BBC resource", 3);

			BlitzModule module = new BlitzModule(this, bbcCode);

			SetTask("Disassembling");
			Disassemble(module);

			SetTask("Decompiling");
			Decompile(module);

			SetTask("Writing Output");
			Directory.CreateDirectory(OutputPath);

			using (StreamWriter sw = new StreamWriter(OutputPath + "test.asm"))
			{
				foreach (var func in module.GetKnownFunctions())
				{
					if (_disassembledFunctions.ContainsKey(func))
						sw.WriteLine(_disassembledFunctions[func]);
				}

				foreach (var pair in module.GetVariables())
				{
					sw.WriteLine(pair.Key + ":");
					if (!pair.Value.StartsWith("    "))
						sw.Write("    ");
					sw.WriteLine(pair.Value);
				}
			}

			Exit("Done");
		}

		private void Disassemble(BlitzModule module)
		{
			Dictionary<string, string> doneFuncs = new Dictionary<string, string>();

			_disassembledFunctions.Add("__MAIN", module.DisassembleFunction("__MAIN", ref doneFuncs, false));

			foreach (var func2 in doneFuncs)
			{
				if (!_disassembledFunctions.ContainsKey(func2.Key))
					_disassembledFunctions.Add(func2.Key, func2.Value);
			}

			while (true)
			{
				int oldLen = module.GetKnownFunctions().Length;
				foreach (var func in module.GetKnownFunctions())
				{
					if (!_disassembledFunctions.ContainsKey(func))
					{
						doneFuncs.Clear();
						_disassembledFunctions.Add(func, module.DisassembleFunction(func, ref doneFuncs, false));

						foreach (var func2 in doneFuncs)
						{
							if (!_disassembledFunctions.ContainsKey(func2.Key))
								_disassembledFunctions.Add(func2.Key, func2.Value);
						}
					}
				}

				if (module.GetKnownFunctions().Length <= oldLen)
					break;
			}

			SetTask("Processing Variables");
			module.ProcessVariables();
			foreach (var pair in module.GetVariables())
			{
				// TEMP: Figure out why variables are sometimes decompiled too
				string name = pair.Key;
				if (_disassembledFunctions.ContainsKey(name))
					_disassembledFunctions.Remove(name);
			}
		}

		private void Decompile(BlitzModule module)
		{

		}
	}
}
