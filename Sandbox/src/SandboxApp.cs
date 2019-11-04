using BootEngine;
using BootEngine.Events;
using BootEngine.Layers;
using Platforms.Windows;

namespace Sandbox
{
	internal class TestLayer : LayerBase
	{
		public TestLayer() : base("TestLayer") { }

		public override void OnUpdate()
		{
			//
		}

		public override void OnEvent(EventBase @event)
		{
			//
		}
	}

	public class SandboxApp : Application<WindowsWindow>
	{
		public static void Main()
		{
			var app = new SandboxApp();
			app.LayerStack.PushLayer(new TestLayer());
			app.Run();
			app.Dispose();
		}
	}
}
