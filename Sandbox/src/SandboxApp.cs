using BootEngine;
using BootEngine.Log;
using System;

namespace Sandbox
{
	public class SandboxApp : Application
	{
		public static void Main(string[] args)
		{
			Logger.Init();
			Logger.CoreError("Hi from Core");
			Logger.Info("Test");

			var app = new SandboxApp();
			app.Run();
			app.Dispose();
		}
	}
}
