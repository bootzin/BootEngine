using BootEngine;
using BootEngine.AssetsManager;
using BootEngine.Events;
using BootEngine.Layers;
using BootEngine.Renderer;
using BootEngine.Renderer.Cameras;
using BootEngine.Utils.ProfilingTools;
using ImGuiNET;
using System.Linq;
using System.Numerics;
using Veldrid;

namespace Sandbox.Layers
{
	public class Sandbox2DLayer : LayerBase
	{
		#region Properties
		private OrthoCameraController _cameraController;
		private Vector4 _squareColor = RgbaFloat.DarkRed.ToVector4();
		private readonly float[] _frametime = new float[100];
		private int _instanceCount = 10;
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

			Renderable2DParameters param = new Renderable2DParameters();
			param.Name = "Quad";
			param.Position = new Vector3(-2, 0, .5f);
			param.Size = new Vector2(.5f, .5f);
			param.Rotation = 0;
			param.Color = _squareColor;
			param.Texture = null;
			Renderer2D.Instance.SubmitQuadDraw(ref param);

			//param.Name = null;
			//param.Position = new Vector3(0, 0, .4f);
			//param.Size = new Vector2(.25f, .25f);
			//param.Rotation = 0f;
			//param.Color = RgbaFloat.White.ToVector4();
			//param.Texture = AssetManager.LoadTexture2D("assets/textures/sampleFly.png", TextureUsage.Sampled);
			//Renderer2D.Instance.SubmitQuadDraw(ref param);

			param.Name = "Quad2";
			param.Position = new Vector3(-1, 0, .5f);
			param.Size = Vector2.One;
			param.Rotation = 0f;
			param.Color = RgbaFloat.Cyan.ToVector4();
			param.Texture = null;
			Renderer2D.Instance.SubmitQuadDraw(ref param);

			param.Name = null;
			param.Size = new Vector2(.1f, .1f);
			param.Rotation = (float)Utils.Util.Deg2Rad(45);
			param.Color = _squareColor;
			param.Texture = null;
			for (int i = 0; i < _instanceCount; i++)
			{
				param.Position = new Vector3(-.11f * i, 0, .5f);
				Renderer2D.Instance.SubmitQuadDraw(ref param);
			}
		}

		public override void OnUpdate(float deltaSeconds)
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			_frametime[99] = deltaSeconds * 1000;
			for (int i = 0; i < 99;)
			{
				_frametime[i] = _frametime[++i];
			}

			_cameraController.Update(deltaSeconds);
			Renderer2D renderer = Renderer2D.Instance;
			renderer.BeginScene(_cameraController.Camera);
			//renderer.UpdateColor("Quad", _squareColor);
			//renderer.UpdatePosition("Quad2", new Vector3(_squareColor.X, _squareColor.Y, _squareColor.Z));
			//renderer.UpdateRotation("Quad2", (float)Utils.Util.Deg2Rad(temp++));

			if (renderer.InstanceCount - 3 < _instanceCount)
			{
				var param = new Renderable2DParameters();
				param.Size = new Vector2(.1f, .1f);
				param.Rotation = (float)Utils.Util.Deg2Rad(45);
				param.Color = _squareColor;
				for (int i = renderer.InstanceCount - 3; i < _instanceCount; i++)
				{
					param.Position = new Vector3(-.11f * i, 0, .5f);
					renderer.SubmitQuadDraw(ref param);
				}
			}
			else if (renderer.InstanceCount - 3 > _instanceCount)
			{
				for (int i = renderer.InstanceCount; i > _instanceCount;)
					renderer.RemoveQuadDraw(--i);
			}

#if DEBUG
			using (Profiler camProfiler = new Profiler("Rendering"))
#endif
				renderer.Render();
			renderer.EndScene();
		}

		public override void OnGuiRender()
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			ImGui.Begin("Settings 2D");
			ImGui.ColorEdit4("Square Color 2D", ref _squareColor);
			ImGui.End();

			ImGui.Begin("FPS Counter");
			ImGui.PlotLines("", ref _frametime[0], 100, 0, "Frametime (ms)", 0, 66.6f, new Vector2(250, 50));
			ImGui.Text("FPS: " + (1000f / _frametime.Average()));
			ImGui.End();

			ImGui.Begin("QuadDraw Config");
			ImGui.DragInt("QuadCount", ref _instanceCount, 1, 10, 200000);
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
