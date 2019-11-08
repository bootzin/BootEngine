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
			var sdlWindow = window.SdlWindow;

			gd = window.GraphicsDevice;
			cl = window.ResourceFactory.CreateCommandList();

			controller = new ImGuiController(gd, gd.SwapchainFramebuffer.OutputDescription, window);

			sdlWindow.Resized += () => controller.WindowResized(sdlWindow.Width, sdlWindow.Height);
		}

		public override void OnDetach()
		{
			controller.Dispose();
		}

		public override void OnUpdate()
		{
			controller.Update(1f / 60);
		}

		public void Begin()
		{
			controller.BeginFrame();
			cl.Begin();
		}

		public void End()
		{
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

		public override void OnGuiRender()
		{
			ImGui.ShowDemoWindow();
		}
		#endregion
	}
}
