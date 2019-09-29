using BootEngine;
using BootEngine.Events;
using BootEngine.Layers;

namespace Sandbox
{
	internal class TestLayer : LayerBase
    {
        public TestLayer() : base("TestLayer") { }

        public override void OnUpdate()
        {
            //Logger.Info("TestLayer Update");
        }

        public override void OnEvent(EventBase @event)
        {
            BootEngine.Log.Logger.Warn(@event);
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
