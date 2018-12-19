namespace BlitzLensLib.Utils
{
	internal static class Logger
	{
		public delegate void OnLogged(LogLevel level, string msg);
		public static event OnLogged Logged;

		public static void Log(LogLevel level, string msg)
		{
			Logged?.Invoke(level, msg);
		}

		public static void Debug(string msg)
		{
			Log(LogLevel.Debug, msg);
		}

		public static void Info(string msg)
		{
			Log(LogLevel.Info, msg);
		}

		public static void Warn(string msg)
		{
			Log(LogLevel.Warn, msg);
		}

		public static void Error(string msg)
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
