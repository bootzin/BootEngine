using BootEngine;
using BootEngine.Utils.ProfilingTools;
using BootEngine.Window;
using Platforms.Windows;
using Shoelace.Layers;

namespace Shoelace
{
	public sealed class Shoelace : Application
	{
		public Shoelace(Veldrid.GraphicsBackend backend) : base(new WindowProps("Shoelace"), typeof(WindowsWindow), backend)
		{
			LayerStack.PushLayer(new EditorLayer());
		}

		public static void Main()
		{
			ProfileWriter.BeginSession("Startup", "BootProfile-Startup.json");
			var app = new Shoelace(Veldrid.GraphicsBackend.Direct3D11);
			ProfileWriter.EndSesison();

			ProfileWriter.BeginSession("Runtime", "BootProfile-Runtime.json");
			app.Run();
			ProfileWriter.EndSesison();
		}
	}
}
