using BootEngine;
using BootEngine.Layers;
using BootEngine.Log;
using BootEngine.Utils.ProfilingTools;
using Platforms.Windows;
using Sandbox.Layers;
using Veldrid;

namespace Sandbox
{
	public sealed class SandboxApp : Application
	{
		public SandboxApp(GraphicsBackend backend) : base(typeof(WindowsWindow), backend)
		{
			//LayerStack.PushLayer(new ExampleLayer());
			LayerStack.PushLayer(new Sandbox2DLayer());
		}

		public static void Main()
		{
			ProfileWriter pw = ProfileWriter.Instance;
			pw.BeginSession("Startup", "BootProfile-Startup.json");
			Logger.Init();
			var app = new SandboxApp(GraphicsBackend.OpenGL);
			pw.EndSesison();

			pw.BeginSession("Runtime", "BootProfile-Runtime.json");
			app.Run();
			pw.EndSesison();

			pw.BeginSession("Shutdown", "BootProfile-Shutdown.json");
			app.Dispose();
			pw.EndSesison();
		}
	}
}
