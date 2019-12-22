using BootEngine;
using BootEngine.AssetsManager;
using BootEngine.Events;
using BootEngine.Layers;
using BootEngine.Renderer;
using BootEngine.Renderer.Cameras;
using BootEngine.Utils.ProfilingTools;
using ImGuiNET;
using System.Numerics;
using Veldrid;

namespace Sandbox.Layers
{
	public class Sandbox2DLayer : LayerBase
	{
		#region Properties
		private OrthoCameraController _cameraController;
		private Renderable2D renderable;
		private Renderable2D renderable2;
		private Vector4 _squareColor = RgbaFloat.DarkRed.ToVector4();
		#endregion

		#region Constructor
		public Sandbox2DLayer() : base("Sanbox2D") { }
		#endregion

		public override void OnAttach()
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			GraphicsDevice _graphicsDevice = Application.App.Window.GraphicsDevice;
			float aspectRatio = (float)Application.App.Window.SdlWindow.Width / Application.App.Window.SdlWindow.Height;
			_cameraController = new OrthoCameraController(aspectRatio, _graphicsDevice.IsDepthRangeZeroToOne, _graphicsDevice.IsClipSpaceYInverted, true);
			renderable = Renderer2D.Instance.SubmitQuadDraw(new Vector3(-2, 0, .5f), new Vector2(.5f, .5f), _squareColor);
			for (int i = 0; i < 1000; i++)
				renderable = Renderer2D.Instance.SubmitQuadDraw(new Vector3(-.11f * i, 0, .5f), new Vector2(.1f, .1f), _squareColor);
			renderable2 = Renderer2D.Instance.SubmitQuadDraw(new Vector3(-1, 0, .5f), Vector2.One, RgbaFloat.Cyan.ToVector4());
			Renderer2D.Instance.SubmitTexture(new Vector3(0, 0, .4f), new Vector2(.25f,.25f), AssetManager.LoadTexture2D("assets/textures/sampleFly.png", TextureUsage.Sampled));
		}

		public override void OnUpdate(float deltaSeconds)
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			_cameraController.Update(deltaSeconds);
			Renderer2D.Instance.BeginScene(_cameraController.Camera);
			Renderer2D.Instance.UpdateBuffer(renderable.ColorBuffer, _squareColor);
			Renderer2D.Instance.UpdateBuffer(renderable2.TransformBuffer, Matrix4x4.CreateTranslation(new Vector3(_squareColor.X, _squareColor.Y, _squareColor.Z)));
#if DEBUG
			using (Profiler camProfiler = new Profiler("Rendering"))
#endif
				Renderer2D.Instance.Render();
			Renderer2D.Instance.EndScene();
		}

		public override void OnGuiRender()
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
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
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			Renderer2D.Instance.Dispose();
		}
	}
}
