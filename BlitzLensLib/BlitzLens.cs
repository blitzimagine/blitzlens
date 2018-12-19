using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlitzLensLib.Decompilers;
using BlitzLensLib.Structures;
using BlitzLensLib.Utils;

namespace BlitzLensLib
{
	public class BlitzLens
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

		public BlitzLens(string inputPath, string outputPath)
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
			byte[] resource = ResourceHelper.GetBlitzCodeFromExecutable(InputPath);
			if (resource == null)
				Exit("Failed to extract BBC resource", 2);

			SetTask("Parsing BBC Resource");
			CodeResource bbcCode = CodeResource.FromBytes(this, resource);
			if (bbcCode == null)
				Exit("Failed to parse BBC resource", 3);

			BlitzDisassembler disassembler = new BlitzDisassembler(this, bbcCode, true);

			SetTask("Disassembling Variables");
			DisassembleVariables(disassembler);

			SetTask("Disassembling Code");
			DisassembleCode(disassembler);

			BlitzDecompiler decompiler = new BlitzDecompiler(this, disassembler);

			SetTask("Decompiling");
			Decompile(decompiler);

			SetTask("Writing Output");
			Directory.CreateDirectory(OutputPath);

			using (StreamWriter sw = new StreamWriter(OutputPath + "test.asm"))
			{
				bool first = true;
				foreach (var pair in disassembler.GetDisassembly())
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

				foreach (var pair in disassembler.GetVariables())
				{
					sw.WriteLine(pair.Key + ":");
					if (!pair.Value.StartsWith("    "))
						sw.Write("    ");
					sw.WriteLine(pair.Value);
				}
			}

			Exit("Done");
		}

		private void DisassembleCode(BlitzDisassembler module)
		{
			module.Disassemble();
		}

		private void DisassembleVariables(BlitzDisassembler module)
		{
			module.ProcessVariables();
		}

		private void Decompile(BlitzDecompiler decompiler)
		{
			decompiler.Decompile();
		}
	}
}
