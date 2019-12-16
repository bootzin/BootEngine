using BootEngine;
using BootEngine.Layers;
using BootEngine.Log;
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
			Logger.Init();
			var app = new SandboxApp(GraphicsBackend.Direct3D11);
			app.Run();
			app.Dispose();
		}
	}
}
