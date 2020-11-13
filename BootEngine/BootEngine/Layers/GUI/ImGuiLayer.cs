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
		public static ImGuiController Controller { get; private set; }
		public static bool BlockEvents { get; set; }
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
			gd = Application.App.Window.GraphicsDevice;
			cl = Application.App.Window.GraphicsDevice.ResourceFactory.CreateCommandList();

			Controller = new ImGuiController(gd, gd.SwapchainFramebuffer.OutputDescription, Application.App.Window);

			Application.App.Window.SdlWindow.Resized += () => Controller.WindowResized(Width, Height, Application.App.Window.SdlWindow.WindowState == WindowState.Minimized);
		}

		public override void OnDetach()
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			Controller.Dispose();
			cl.Dispose();
		}

		public void Begin(float deltaSeconds)
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			Controller.Update(deltaSeconds);
			Controller.BeginFrame();
			cl.Begin();
		}

		public void End()
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			cl.SetFramebuffer(gd.SwapchainFramebuffer);
			cl.ClearDepthStencil(1f);
			cl.ClearColorTarget(0, RgbaFloat.Grey);
			Controller.Render(gd, cl);

			cl.End();

			gd.SubmitCommands(cl);
			Controller.SwapBuffers(gd);
		}

		public override void OnEvent(EventBase @event)
		{
			if (BlockEvents)
			{
				var io = ImGuiNET.ImGui.GetIO();
				@event.Handled |= @event.IsInCategory(EventCategory.Mouse) && io.WantCaptureMouse;
				@event.Handled |= @event.IsInCategory(EventCategory.Keyboard) && io.WantCaptureKeyboard;
			}
		}
		#endregion
	}
}
