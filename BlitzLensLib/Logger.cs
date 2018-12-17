using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlitzLensLib
{
	public class Logger
	{
		public delegate void OnLogged(LogLevel level, string msg);
		public event OnLogged Logged;

		public Logger()
		{

		}

		public void Log(LogLevel level, string msg)
		{
			Logged?.Invoke(level, msg);
		}

		public void Debug(string msg)
		{
			Log(LogLevel.Debug, msg);
		}

		public void Info(string msg)
		{
			Log(LogLevel.Info, msg);
		}

		public void Warn(string msg)
		{
			Log(LogLevel.Warn, msg);
		}

		public void Error(string msg)
		{
			Log(LogLevel.Error, msg);
		}
	}

	public enum LogLevel
	{
		Debug,
		Info,
		Warn,
		Error
	}
}
