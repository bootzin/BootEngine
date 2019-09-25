using BootEngine;
using BootEngine.Events;
using BootEngine.Layers;
using BootEngine.Log;

namespace Sandbox
{
    internal class TestLayer : Layer
    {
        public TestLayer() : base("TestLayer") { }

        public override void OnUpdate()
        {
            Logger.Info("TestLayer Update");
        }

        public override void OnEvent(EventBase @event)
        {
            Logger.Fatal(@event);
        }
    }

	public class SandboxApp : Application<Platforms.Windows.WindowsWindow>
	{
		public static void Main(string[] args)
		{
			var app = new SandboxApp();
            app.LayerStack.PushLayer(new TestLayer());
			app.Run();
			app.Dispose();
		}
	}
}
