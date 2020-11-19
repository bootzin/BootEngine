using BootEngine;
using BootEngine.Layers;
using BootEngine.Log;
using BootEngine.Utils;
using BootEngine.Utils.ProfilingTools;
using Platforms.Windows;
using Sandbox.Layers;

namespace Sandbox
{
	public sealed class SandboxApp : Application
	{
		public SandboxApp(Veldrid.GraphicsBackend backend) : base(typeof(WindowsWindow), backend)
		{
			//LayerStack.PushLayer(new ExampleLayer());
			LayerStack.PushLayer(new Sandbox2DLayer());
		}

		public static void Main()
		{
			ProfileWriter.BeginSession("Startup", "BootProfile-Startup.json");
			Logger.Init();
			var app = new SandboxApp(Veldrid.GraphicsBackend.Direct3D11);
			ProfileWriter.EndSesison();

			ProfileWriter.BeginSession("Runtime", "BootProfile-Runtime.json");
			app.Run();
			ProfileWriter.EndSesison();

			ProfileWriter.BeginSession("Shutdown", "BootProfile-Shutdown.json");
			app.Dispose();
			ProfileWriter.EndSesison();
		}
	}
}
