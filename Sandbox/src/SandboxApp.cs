using BootEngine;
using BootEngine.Log;
using System;
using BootEngine.Events;

namespace Sandbox
{
	public class SandboxApp : Application<Platforms.Windows.WindowsWindow>
	{
		public static void Main(string[] args)
		{
			var app = new SandboxApp();
			app.Run();
			app.Dispose();
		}
	}
}
