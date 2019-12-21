using BootEngine.Events;
using BootEngine.Utils.ProfilingTools;
using Veldrid;

namespace BootEngine.Layers.GUI
{
	public sealed class ImGuiLayer : LayerBase
	{
		#region Properties
		private GraphicsDevice gd;
		private CommandList cl;
		private ImGuiController controller;
		#endregion

		#region Constructor
		public ImGuiLayer() : base("GUI Layer") { }
		#endregion

		#region Methods
		public override void OnAttach()
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			var window = Application.App.Window;
			var sdlWindow = window.SdlWindow;

			gd = window.GraphicsDevice;
			cl = window.ResourceFactory.CreateCommandList();

			controller = new ImGuiController(gd, gd.SwapchainFramebuffer.OutputDescription, window);

			sdlWindow.Resized += () => controller.WindowResized(sdlWindow.Width, sdlWindow.Height, sdlWindow.WindowState == WindowState.Minimized);
		}

		public override void OnDetach()
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			controller.Dispose();
			cl.Dispose();
		}

		public void Begin(float deltaSeconds)
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			controller.Update(deltaSeconds);
			controller.BeginFrame();
			cl.Begin();
		}

		public void End()
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			cl.SetFramebuffer(gd.SwapchainFramebuffer);
			controller.Render(gd, cl);

			cl.End();

			gd.SubmitCommands(cl);
			controller.SwapBuffers(gd);
		}

		public override void OnEvent(EventBase @event)
		{
			//
		}
		#endregion
	}
}
