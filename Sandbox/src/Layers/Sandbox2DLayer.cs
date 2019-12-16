using BootEngine;
using BootEngine.Events;
using BootEngine.Layers;
using BootEngine.Renderer;
using BootEngine.Renderer.Cameras;
using ImGuiNET;
using System.Numerics;
using Veldrid;

namespace Sandbox.Layers
{
	public class Sandbox2DLayer : LayerBase
	{
		#region Properties
		private OrthoCameraController _cameraController;
		private Vector4 _squareColor = RgbaFloat.DarkRed.ToVector4();
		#endregion

		#region Constructor
		public Sandbox2DLayer() : base("Sanbox2D") { }
		#endregion

		public override void OnAttach()
		{
			var _graphicsDevice = Application.App.Window.GraphicsDevice;
			float aspectRatio = (float)Application.App.Window.SdlWindow.Width / Application.App.Window.SdlWindow.Height;
			_cameraController = new OrthoCameraController(aspectRatio, _graphicsDevice.IsDepthRangeZeroToOne, _graphicsDevice.IsClipSpaceYInverted, true);
			Renderer2D.Instance.SubmitQuadDraw(new Vector3(0, 0, 0), Vector2.One, _squareColor);
			Renderer2D.Instance.SubmitQuadDraw(new Vector3(0, 0, 0), new Vector2(.5f,.5f), RgbaFloat.Cyan.ToVector4());
		}

		public override void OnUpdate(float deltaSeconds)
		{
			_cameraController.Update(deltaSeconds);
			Renderer2D.Instance.BeginScene(_cameraController.Camera);
			Renderer2D.Instance.Render();
			Renderer2D.Instance.EndScene();
		}

		public override void OnGuiRender()
		{
			ImGui.Begin("Settings 2D");
			ImGui.ColorEdit4("Square Color 2D", ref _squareColor);
			ImGui.End();
		}

		public override void OnEvent(EventBase @event)
		{
			_cameraController.OnEvent(@event);
		}

		public override void OnDetach()
		{
			Renderer2D.Instance.Dispose();
		}
	}
}
