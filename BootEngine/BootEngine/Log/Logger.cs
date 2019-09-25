using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using System;

namespace BootEngine.Log
{
	public static class Logger
	{
		#region Properties
		private static ILogger CoreLogger { get; set; }
		private static ILogger ClientLogger { get; set; }
		#endregion

		[System.Diagnostics.Conditional("DEBUG")]
		public static void Init()
		{
            CoreLogger = new LoggerConfiguration()
				.WriteTo.Console(
					outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}]: {Message:lj}{NewLine}{Exception}",
					theme: AnsiConsoleTheme.Literate)
				.MinimumLevel.Verbose()
				.CreateLogger();

			ClientLogger = new LoggerConfiguration()
				.WriteTo.Console(
					outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}]: {Message:lj}{NewLine}{Exception}",
					theme: AnsiConsoleTheme.Literate)
				.MinimumLevel.Verbose()
				.CreateLogger();
        }

        #region ClientLogger
        [System.Diagnostics.Conditional("DEBUG")]
		public static void Error(object message, Exception ex = null)
		{
			if (message is string)
				ClientLogger.Error(ex, message as string);
			else
				ClientLogger.Error(ex, message.ToString());
		}

		[System.Diagnostics.Conditional("DEBUG")]
		public static void Debug(object message)
		{
			if (message is string)
				ClientLogger.Debug(message as string);
			else
				ClientLogger.Debug(message.ToString());
		}

		[System.Diagnostics.Conditional("DEBUG")]
		public static void Verbose(object message)
		{
			if (message is string)
				ClientLogger.Verbose(message as string);
			else
				ClientLogger.Verbose(message.ToString());
		}

		[System.Diagnostics.Conditional("DEBUG")]
		public static void Warn(object message)
		{
			if (message is string)
				ClientLogger.Warning(message as string);
			else
				ClientLogger.Warning(message.ToString());
		}

		[System.Diagnostics.Conditional("DEBUG")]
		public static void Info(object message)
		{
			if (message is string)
				ClientLogger.Information(message as string);
			else
				ClientLogger.Information(message.ToString());
		}
		#endregion

		#region CoreLogger
		[System.Diagnostics.Conditional("DEBUG")]
		public static void CoreError(object message, Exception ex = null)
		{
			if (message is string)
				CoreLogger.Error(ex, message as string);
			else
				CoreLogger.Error(ex, message.ToString());
		}

		[System.Diagnostics.Conditional("DEBUG")]
		public static void CoreDebug(object message)
		{
			if (message is string)
				CoreLogger.Debug(message as string);
			else
				CoreLogger.Debug(message.ToString());
		}

		[System.Diagnostics.Conditional("DEBUG")]
		public static void CoreVerbose(object message)
		{
			if (message is string)
				CoreLogger.Verbose(message as string);
			else
				CoreLogger.Verbose(message.ToString());
		}

		[System.Diagnostics.Conditional("DEBUG")]
		public static void CoreWarn(object message)
		{
			if (message is string)
				CoreLogger.Warning(message as string);
			else
				CoreLogger.Warning(message.ToString());
		}

		[System.Diagnostics.Conditional("DEBUG")]
		public static void CoreInfo(object message)
		{
			if (message is string)
				CoreLogger.Information(message as string);
			else
				CoreLogger.Information(message.ToString());
		}
		#endregion
	}
}