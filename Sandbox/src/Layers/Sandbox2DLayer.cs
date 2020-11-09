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
using System.Threading.Tasks;
using Veldrid;

namespace Sandbox.Layers
{
	public class Sandbox2DLayer : LayerBase
	{
		#region Properties
		private OrthoCameraController cameraController;
		private Vector4 squareColor = RgbaFloat.DarkRed.ToVector4();
		private float temp;
		private int instanceCount = 10;
		private readonly float[] _frametime = new float[100];
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
			cameraController = new OrthoCameraController(aspectRatio, _graphicsDevice.IsDepthRangeZeroToOne, _graphicsDevice.IsClipSpaceYInverted, true);

			Renderable2DParameters param = new Renderable2DParameters();
			param.Name = "Quad";
			param.Position = new Vector3(-1, 0, .5f);
			param.Size = new Vector2(.5f, .5f);
			param.Rotation = 0;
			param.Color = squareColor;
			param.Texture = null;
			Renderer2D.Instance.SetupQuadDraw(ref param);

			Renderable2DParameters param1 = new Renderable2DParameters();
			param1.Name = "Tex";
			param1.Position = new Vector3(0, 0, .4f);
			param1.Size = new Vector2(.25f, .25f);
			param1.Rotation = 0f;
			param1.Color = RgbaFloat.White.ToVector4();
			param1.Texture = AssetManager.LoadTexture2D("assets/textures/sampleFly.png", TextureUsage.Sampled);
			Renderer2D.Instance.SetupQuadDraw(ref param1);

			Renderable2DParameters param2 = new Renderable2DParameters();
			param2.Name = "Quad2";
			param2.Position = new Vector3(1, 0, .5f);
			param2.Size = Vector2.One;
			param2.Rotation = 0f;
			param2.Color = RgbaFloat.Cyan.ToVector4();
			param2.Texture = null;
			Renderer2D.Instance.SetupQuadDraw(ref param2);

			Renderable2DParameters param3 = new Renderable2DParameters();
			param3.Name = null;
			param3.Size = new Vector2(.1f, .1f);
			param3.Rotation = 0;
			param3.Color = squareColor;
			//param3.Texture = null;
			param3.Texture = AssetManager.LoadTexture2D("assets/textures/sampleFly.png", TextureUsage.Sampled);
			for (int i = 0; i < instanceCount; i++)
			{
				param3.Position = new Vector3(-.11f * (i % 1000), -.11f * (i / 1000), .5f);
				Renderer2D.Instance.SetupQuadDraw(ref param3);
			}

			Renderer2D.Instance.Flush();
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

			cameraController.Update(deltaSeconds);
			Renderer2D renderer = Renderer2D.Instance;
			renderer.BeginScene(cameraController.Camera);
			renderer.UpdatePosition("Quad2", new Vector3(squareColor.X, squareColor.Y, squareColor.Z));

#if DEBUG
			using (Profiler updateProfiler = new Profiler("Update"))
#endif
				Parallel.For(0, renderer.InstanceCount, (i) =>
				{
					renderer.UpdateColor(i, squareColor);
					renderer.UpdateRotation(i, Utils.Util.Deg2Rad(temp));
				});
				//for (int i = 0; i < renderer.InstanceCount; i++)
				//{
				//	renderer.UpdateColor(i, _squareColor);
				//	renderer.UpdateRotation(i, (float)Utils.Util.Deg2Rad(temp));
				//}
			temp++;

			if (renderer.InstanceCount < instanceCount)
			{
				var param = new Renderable2DParameters();
				param.Size = new Vector2(.1f, .1f);
				param.Rotation = 0;
				param.Color = squareColor;
				param.Texture = AssetManager.LoadTexture2D("assets/textures/sampleDog.png", TextureUsage.Sampled);
				for (int i = renderer.InstanceCount; i < instanceCount; i++)
				{
						param.Position = new Vector3(-.11f * (i % 1000), -.11f * (i / 1000), .5f);
						renderer.SetupQuadDraw(ref param);
				}
				renderer.Flush();
			}
			else if (renderer.InstanceCount > instanceCount)
			{
				for (int i = renderer.InstanceCount; i > instanceCount;)
					renderer.RemoveQuadDraw(--i);
				renderer.Flush();
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
			ImGui.ColorEdit4("Square Color 2D", ref squareColor);
			ImGui.End();

			ImGui.Begin("FPS Counter");
			ImGui.PlotLines("", ref _frametime[0], 100, 0, "Frametime (ms)", 0, 66.6f, new Vector2(250, 50));
			ImGui.Text("FPS: " + (1000f / _frametime.Average()));
			ImGui.End();

			ImGui.Begin("QuadDraw Config");
			ImGui.DragInt("QuadCount", ref instanceCount, 1, 0, 3000000);
			ImGui.End();
		}

		public override void OnEvent(EventBase @event)
		{
			cameraController.OnEvent(@event);
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
