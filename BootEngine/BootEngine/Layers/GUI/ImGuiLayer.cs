using BootEngine.Utils;
using BootEngine.Utils.ProfilingTools;
using Veldrid;

namespace BootEngine.Layers.GUI
{
	public sealed class ImGuiLayer : LayerBase
	{
		#region Properties
		private GraphicsDevice gd;
		private CommandList cl;
		internal static ImGuiController Controller { get; private set; }
		public static bool ShouldClearBuffers { get; set; }
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
				cl.ClearColorTarget(0, ColorF.DarkGrey);
			}
			Controller.Render(gd, cl);

			cl.End();

			gd.SubmitCommands(cl);
			Controller.SwapBuffers(gd);
		}

		public static System.IntPtr GetOrCreateImGuiBinding(ResourceFactory factory, Texture texture) => Controller.GetOrCreateImGuiBinding(factory, texture);
		public static void LoadFonts(ImGuiFontInfo[] infos) => Controller.LoadFonts(Application.App.Window.GraphicsDevice, infos);
		#endregion
	}
}
