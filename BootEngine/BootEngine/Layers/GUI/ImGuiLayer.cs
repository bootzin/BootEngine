using BootEngine.Events;
using ImGuiNET;
using System;
using System.Runtime.InteropServices;
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
			var sdlWindow = window.GetSdlWindow();

			gd = window.GetGraphicsDevice();
			cl = window.ResourceFactory.CreateCommandList();

			controller = new ImGuiController(gd, gd.MainSwapchain.Framebuffer.OutputDescription, window);

			sdlWindow.Resized += () => controller.WindowResized(sdlWindow.Width, sdlWindow.Height);
		}

		public override void OnDetach()
		{
			controller.Dispose();
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
		}

		public void End()
		{
			cl.SetFramebuffer(gd.MainSwapchain.Framebuffer);
			cl.ClearColorTarget(0, new RgbaFloat(0.45f, 0.55f, 0.6f, 1f));
			controller.Render(gd, cl);

			cl.End();
			gd.SubmitCommands(cl);
			controller.SwapBuffers(gd);
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
