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

		public BlitzDecompiler(string inputPath, string outputPath)
		{
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
			string minimum2Asm = module.DisassembleFunction(0xF30C);//"_fupdategame");
			Logger.Debug(minimum2Asm);

			Exit("Done");
		}
	}
}
