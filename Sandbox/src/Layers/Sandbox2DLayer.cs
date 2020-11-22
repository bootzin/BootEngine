using BootEngine;
using BootEngine.Utils;
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
using BootEngine.ECS.Systems;
using BootEngine.ECS.Components;
using Leopotam.Ecs;

namespace Sandbox.Layers
{
	 //TODO: Adjust Sandbox2DLayer Layer to conform with new ECS patterns
	public class Sandbox2DLayer : LayerBase
	{
		#region Properties
		private Vector4 squareColor = ColorF.DarkRed;
		private float rot;
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
			ActiveScene
				.AddSystem(new VelocitySystem())
				.Init();

			var cam = ActiveScene.CreateEntity("Main Camera");
			var camera = new OrthoCamera(1, -1, 1);
			camera.ResizeViewport(Width, Height);
			cam.AddComponent(new CameraComponent()
			{
				Camera = camera
			});
			cam.AddComponent<VelocityComponent>();

			var quad = ActiveScene.CreateEntity("Quad");
			quad.AddComponent(new SpriteComponent()
			{
				Color = squareColor
			});
			ref var transform = ref quad.GetComponent<TransformComponent>();
			transform.Position = new Vector3(-1, 0, .5f);
			transform.Scale = new Vector3(.5f, .5f, 1);
			transform.Rotation = Vector3.Zero;

			var quad2 = ActiveScene.CreateEntity(quad, "Tex");
			ref var transform2 = ref quad2.GetComponent<TransformComponent>();
			transform2.Position = new Vector3(0, 0, .4f);
			transform2.Scale = new Vector3(.25f, .25f, 1);
			transform2.Rotation = Vector3.Zero;
			ref var sprite = ref quad2.GetComponent<SpriteComponent>();
			sprite.Color = ColorF.White;
			sprite.Texture = AssetManager.LoadTexture2D("assets/textures/sampleFly.png", TextureUsage.Sampled);

			var quad3 = ActiveScene.CreateEntity(quad, "Quad2");
			ref var transform3 = ref quad3.GetComponent<TransformComponent>();
			transform3.Position = new Vector3(1, 0, .5f);
			transform3.Scale = Vector3.One;
			transform3.Rotation = Vector3.Zero;
			ref var sprite2 = ref quad2.GetComponent<SpriteComponent>();
			sprite2.Color = ColorF.Cyan;

			var quad4 = ActiveScene.CreateEntity(quad);
			ref var transform4 = ref quad4.GetComponent<TransformComponent>();
			transform4.Position = new Vector3(1, 0, .5f);
			transform4.Scale = Vector3.One * .1f;
			transform4.Rotation = Vector3.Zero;
			ref var sprite3 = ref quad2.GetComponent<SpriteComponent>();
			sprite3.Color = squareColor;
			sprite.Texture = AssetManager.LoadTexture2D("assets/textures/sampleFly.png", TextureUsage.Sampled);

			for (int i = 0; i < instanceCount; i++)
			{
				var quad5 = ActiveScene.CreateEntity(quad4);
				ref var transform5 = ref quad5.GetComponent<TransformComponent>();
				transform5.Position = new Vector3(-.11f * (i % 1000), -.11f * (i / 1000), .5f);
			}
		}

		public override void OnUpdate(float deltaSeconds)
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			Renderer2D.Instance.ResetStats();
			_frametime[99] = deltaSeconds * 1000;
			for (int i = 0; i < 99;)
			{
				_frametime[i] = _frametime[++i];
			}

			var filter = (EcsFilter<TransformComponent, TagComponent, SpriteComponent>)ActiveScene.GetFilter(typeof(EcsFilter<TransformComponent, TagComponent, SpriteComponent>));
			foreach (var item in filter)
			{
				if (filter.Get2(item).Tag == "Quad2")
				{
					ref var transform = ref filter.Get1(item);
					transform.Position = new Vector3(squareColor.X, squareColor.Y, squareColor.Z);
				}
			}

#if DEBUG
			using (Profiler updateProfiler = new Profiler("Update"))
#endif
				foreach (int item in filter)
				{
					ref var transform = ref filter.Get1(item);
					ref var sprite = ref filter.Get3(item);
					transform.Rotation = new Vector3(0, 0, Util.Deg2Rad(rot));
					sprite.Color = squareColor;
				}
			rot++;

			ActiveScene.Update();

			//if (renderer.InstanceCount < instanceCount)
			//{
			//	var param = new Renderable2DParameters();
			//	param.Size = new Vector2(.1f, .1f);
			//	param.Rotation = 0;
			//	param.Color = squareColor;
			//	param.Texture = AssetManager.LoadTexture2D("assets/textures/sampleDog.png", TextureUsage.Sampled);
			//	for (int i = renderer.InstanceCount; i < instanceCount; i++)
			//	{
			//		param.Position = new Vector3(-.11f * (i % 1000), -.11f * (i / 1000), .5f);
			//		renderer.SetupQuadDraw(ref param);
			//	}
			//}
			//else if (renderer.InstanceCount > instanceCount)
			//{
			//	for (int i = renderer.InstanceCount; i > instanceCount;)
			//		renderer.RemoveQuadDraw(--i);
			//}
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
			ImGui.Text("FPS: " + (1000f / _frametime.Average()).ToString());
			ImGui.End();

			ImGui.Begin("QuadDraw Config");
			ImGui.DragInt("QuadCount", ref instanceCount, 1, 0, 3000000);
			ImGui.End();
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
