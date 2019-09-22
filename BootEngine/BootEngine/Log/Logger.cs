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

		public static void Init()
		{
			CoreLogger = new LoggerConfiguration()
				.WriteTo.Console(
					restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Verbose,
					outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}]: {Message:lj}{NewLine}{Exception}",
					theme: AnsiConsoleTheme.Literate).CreateLogger();

			ClientLogger = new LoggerConfiguration()
				.WriteTo.Console(
					restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Verbose,
					outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}]: {Message:lj}{NewLine}{Exception}",
					theme: AnsiConsoleTheme.Literate).CreateLogger();
		}

		#region ClientLogger
		[System.Diagnostics.Conditional("DEBUG")]
		public static void Error(string message, Exception ex = null)
		{
			ClientLogger.Error(ex, message);
		}

		[System.Diagnostics.Conditional("DEBUG")]
		public static void Debug(string message)
		{
			ClientLogger.Debug(message);
		}

		[System.Diagnostics.Conditional("DEBUG")]
		public static void Verbose(string message)
		{
			ClientLogger.Verbose(message);
		}

		[System.Diagnostics.Conditional("DEBUG")]
		public static void Warn(string message)
		{
			ClientLogger.Warning(message);
		}

		[System.Diagnostics.Conditional("DEBUG")]
		public static void Info(string message)
		{
			ClientLogger.Information(message);
		}
		#endregion

		#region CoreLogger
		[System.Diagnostics.Conditional("DEBUG")]
		public static void CoreError(string message, Exception ex = null)
		{
			CoreLogger.Error(ex, message);
		}

		[System.Diagnostics.Conditional("DEBUG")]
		public static void CoreDebug(string message)
		{
			CoreLogger.Debug(message);
		}

		[System.Diagnostics.Conditional("DEBUG")]
		public static void CoreVerbose(string message)
		{
			CoreLogger.Verbose(message);
		}

		[System.Diagnostics.Conditional("DEBUG")]
		public static void CoreWarn(string message)
		{
			CoreLogger.Warning(message);
		}

		[System.Diagnostics.Conditional("DEBUG")]
		public static void CoreInfo(string message)
		{
			CoreLogger.Information(message);
		}
		#endregion
	}
}