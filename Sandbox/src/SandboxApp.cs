using System;
using BootEngine;

namespace Sandbox
{
	public class SandboxApp : Application
	{
		public static void Main(string[] args)
		{
			var app = new SandboxApp();
			app.Run();
			app.Dispose();
		}
	}
}
