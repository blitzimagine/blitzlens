using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlitzLensLib;

namespace BlitzLens
{
	public class Program
	{
		private static void Exit(string msg = null, int exitCode = 0)
		{
			if (!string.IsNullOrWhiteSpace(msg))
			{
				if (exitCode == 0)
					Info(msg);
				else
					Error(msg);
			}
			
#if DEBUG
			string code = "ExitCode: " + exitCode;
			if (exitCode == 0)
				Info(code);
			else
				Error(code);

			Console.Write("Press any key to continue...");
			Console.ReadKey();
#endif
			Environment.Exit(exitCode);
		}

		public static void SetTask(string status)
		{
			SetColor(ConsoleColor.Black, ConsoleColor.Green);
			Console.WriteLine("> " + status);
			ResetColor();
		}

		public static void SetHeader(string header)
		{
			SetColor(ConsoleColor.Black, ConsoleColor.White);
			Console.WriteLine("=== " + header + " ===");
			ResetColor();
		}

		public static void Debug(string msg)
		{
			SetColor(ConsoleColor.DarkGreen, ConsoleColor.Yellow);
			Console.WriteLine(msg);
			ResetColor();
		}

		public static void Info(string msg)
		{
			SetColor(ConsoleColor.Black, ConsoleColor.Gray);
			Console.WriteLine(msg);
			ResetColor();
		}

		public static void Warn(string msg)
		{
			SetColor(ConsoleColor.Black, ConsoleColor.Yellow);
			Console.WriteLine(msg);
			ResetColor();
		}

		public static void Error(string msg)
		{
			SetColor(ConsoleColor.Red, ConsoleColor.White);
			Console.WriteLine(msg);
			ResetColor();
		}

		public static void SetColor(ConsoleColor bg, ConsoleColor fg)
		{
			Console.BackgroundColor = bg;
			Console.ForegroundColor = fg;
		}

		public static void ResetColor()
		{
			SetColor(ConsoleColor.Black, ConsoleColor.Gray);
		}

		public static void Main(string[] args)
		{
			if (args.Length < 1)
				Exit("Usage: blitzlens <inputfile> [outputdir]", -1);

			string exePath = args[0];
			string outputDirectory = exePath + "_dump/";
			if (args.Length > 1)
				outputDirectory = args[1];

			BlitzDecompiler decompiler = new BlitzDecompiler(exePath, outputDirectory);
			decompiler.Exited += Exit;
			decompiler.Logged += DecompilerOnLogged;
			decompiler.HeaderSet += SetHeader;
			decompiler.TaskSet += SetTask;
			decompiler.Process();
		}

		private static void DecompilerOnLogged(LogLevel level, string msg)
		{
			switch (level)
			{
				case LogLevel.Debug:
					Debug(msg);
					break;
				case LogLevel.Warn:
					Warn(msg);
					break;
				case LogLevel.Error:
					Error(msg);
					break;
				default:
					Info(msg);
					break;
			}
		}
	}
}
