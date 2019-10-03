using BootEngine.Events;
using ImGuiNET;
using Veldrid;

namespace BootEngine.Layers.GUI
{
	public class ImGuiLayer<WindowType> : LayerBase
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
			var window = Application<WindowType>.App.Window;
			var nativeWindow = window.GetNativeWindow();

			gd = window.GetGraphicsDevice();
			cl = window.ResourceFactory.CreateCommandList();

			controller = new ImGuiController(gd, gd.MainSwapchain.Framebuffer.OutputDescription, nativeWindow);

			window.GetNativeWindow().Resized += () => controller.WindowResized(nativeWindow.Width, nativeWindow.Height);
		}

		public override void OnDetach()
		{
			controller.Dispose();
			cl.Dispose();
			gd.Dispose();
			ImGui.DestroyContext();
		}

		public override void OnUpdate()
		{
			controller.Update(1f / 60);
		}

		public void Begin()
		{
			controller.BeginFrame();
			cl.Begin();
			cl.SetFramebuffer(gd.MainSwapchain.Framebuffer);
			cl.ClearColorTarget(0, new RgbaFloat(0.45f, 0.55f, 0.6f, 1f));
		}

		public void End()
		{
			controller.Render(gd, cl);

			var ctx = ImGui.GetCurrentContext();
			ImGui.UpdatePlatformWindows();
			ImGui.RenderPlatformWindowsDefault();
			ImGui.SetCurrentContext(ctx);

			cl.End();
			gd.SubmitCommands(cl);
			gd.SwapBuffers();
		}

		public override void OnEvent(EventBase @event)
		{
			new EventDispatcher(@event).Dispatch<KeyPressedEvent>((_) => true);
		}

		public override void OnGuiRender()
		{
			ImGui.ShowDemoWindow();
		}
		#endregion
	}
}
