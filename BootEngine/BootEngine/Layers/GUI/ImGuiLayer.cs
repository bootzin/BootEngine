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
		internal static bool ShouldClearBuffers { get; set; }
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
			cl = window.GraphicsDevice.ResourceFactory.CreateCommandList();

			Controller = new ImGuiController(gd, gd.SwapchainFramebuffer.OutputDescription, window);

			sdlWindow.Resized += () => Controller.WindowResized(Width, Height, sdlWindow.WindowState == WindowState.Minimized);
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
			if (ShouldClearBuffers)
			{
				cl.ClearDepthStencil(1f);
				cl.ClearColorTarget(0, RgbaFloat.Grey);
			}
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
