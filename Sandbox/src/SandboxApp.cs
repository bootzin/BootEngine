using BootEngine;
using BootEngine.Log;
using System;
using BootEngine.Events;

namespace Sandbox
{
	public class SandboxApp : Application
	{
		public static void Main(string[] args)
		{
			Logger.Init();
			Logger.CoreError("Hi from Core");
			Logger.Info("Test");

			var e = new WindowResizeEvent(1280, 768);
			Logger.CoreVerbose(e);
			Logger.Info(e);

			if (e.IsInCategory(EventCategory.Application))
			{
				Logger.Warn(e);
			}
			else
			{
				Logger.CoreError(e);
			}

			var app = new SandboxApp();
			app.Run();
			app.Dispose();
		}
	}
}
