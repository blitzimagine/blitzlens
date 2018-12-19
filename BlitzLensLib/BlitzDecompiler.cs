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

		public BlitzDecompiler(string inputPath, string outputPath)
		{
			
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
			BlitzBasicCodeResource bbcCode = BlitzBasicCodeResource.FromBytes(this, resource);
			if (bbcCode == null)
				Exit("Failed to parse BBC resource", 3);

			BlitzModule module = new BlitzModule(this, bbcCode, true);

			SetTask("Disassembling Variables");
			DisassembleVariables(module);

			SetTask("Disassembling Code");
			DisassembleCode(module);

			SetTask("Decompiling");
			Decompile(module);

			SetTask("Writing Output");
			Directory.CreateDirectory(OutputPath);

			using (StreamWriter sw = new StreamWriter(OutputPath + "test.asm"))
			{
				bool first = true;
				foreach (var pair in module.GetDisassembly())
				{
					string name = bbcCode?.GetSymbolName(pair.Key);
					if (first)
						first = false;
					else if (name != null)
						sw.WriteLine();

					if (name != null)
						sw.WriteLine(name + ":");
					sw.WriteLine(pair.Value.Indent());
				}

				sw.WriteLine();

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

		private void DisassembleCode(BlitzModule module)
		{
			module.Disassemble();
		}

		private void DisassembleVariables(BlitzModule module)
		{
			module.ProcessVariables();
		}

		private void Decompile(BlitzModule module)
		{
			// TODO: Decompile back to BlitzBasic
		}
	}
}
