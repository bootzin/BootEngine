﻿using BootEngine.AssetsManager;
using BootEngine.ECS.Components;
using BootEngine.ECS.Components.Events;
using BootEngine.ECS.Systems;
using BootEngine.Events;
using BootEngine.Layers;
using BootEngine.Renderer;
using BootEngine.Renderer.Cameras;
using BootEngine.Utils;
using BootEngine.Utils.ProfilingTools;
using ImGuiNET;
using Sandbox.Services;
using Sandbox.Systems;
using System.Linq;
using System.Numerics;
using Veldrid;

namespace Sandbox.Layers
{
	//TODO: Adjust Sandbox2DLayer Layer to conform with new ECS patterns
	public class Sandbox2DLayer : LayerBase
	{
		#region Properties
		private readonly QuadInfoService _quadData = new QuadInfoService();
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
			_quadData.SquareColor = ColorF.DarkRed;
			_quadData.QuadCount = 10;
			ActiveScene
				.AddSystem(new VelocitySystem())
				.AddSystem(new QuadUpdateSystem())
				.Inject(_quadData)
				.Init();

			LoadStandardShaders();

			var camera = new Camera(false);
			camera.SetPerspective(MathUtil.Deg2Rad(70), .0001f, 1000f);
			camera.ResizeViewport(Width, Height);

			var editorCam = ActiveScene.CreateEmptyEntity();
			editorCam.AddComponent(new CameraComponent()
			{
				Camera = camera
			});
			editorCam.AddComponent(new TransformComponent() { Translation = new Vector3(1e-6f, 1e-6f, 1) });
			camera.BlendState = BlendStateDescription.SingleAlphaBlend;
			camera.DepthStencilState = DepthStencilStateDescription.DepthOnlyLessEqual;
			camera.RasterizerState = RasterizerStateDescription.CullNone;

			camera.RenderTarget = GraphicsDevice.SwapchainFramebuffer;

			var quad = ActiveScene.CreateEntity("Quad");
			quad.AddComponent(new SpriteRendererComponent()
			{
				Color = _quadData.SquareColor,
				Material = new Material("Standard2D"),
				SpriteData = RenderData2D.QuadData
			});
			ref var transform = ref quad.GetComponent<TransformComponent>();
			transform.Translation = new Vector3(-1, 0, .5f);
			transform.Scale = new Vector3(.5f, .5f, 1);
			transform.Rotation = Vector3.Zero;
			quad.AddComponent(new VelocityComponent()
			{
				RotationSpeed = new Vector3(0, 0, 1f)
			});

			var quad2 = ActiveScene.CreateEntity(quad, "Tex");
			ref var transform2 = ref quad2.GetComponent<TransformComponent>();
			transform2.Translation = new Vector3(0, 0, .4f);
			transform2.Scale = new Vector3(.25f, .25f, 1);
			transform2.Rotation = Vector3.Zero;
			ref var sprite = ref quad2.GetComponent<SpriteRendererComponent>();
			sprite.Color = ColorF.White;
			sprite.Material = new Material("Standard2D");
			sprite.SpriteData = RenderData2D.QuadData;
			//sprite.Texture = AssetManager.LoadTexture2D("assets/textures/sampleFly.png", TextureUsage.Sampled);

			var quad3 = ActiveScene.CreateEntity(quad, "Quad2");
			ref var transform3 = ref quad3.GetComponent<TransformComponent>();
			transform3.Translation = new Vector3(1, 0, .5f);
			transform3.Scale = Vector3.One;
			transform3.Rotation = new Vector3(0, 0, 45f);
			ref var sprite2 = ref quad3.GetComponent<SpriteRendererComponent>();
			sprite2.Color = ColorF.Cyan;
			sprite2.Material = new Material("Standard2D");
			sprite2.SpriteData = RenderData2D.QuadData;

			var quad4 = ActiveScene.CreateEntity(quad);
			ref var transform4 = ref quad4.GetComponent<TransformComponent>();
			transform4.Translation = new Vector3(1, 0, .5f);
			transform4.Scale = Vector3.One * .1f;
			transform4.Rotation = Vector3.Zero;
			ref var sprite3 = ref quad4.GetComponent<SpriteRendererComponent>();
			sprite3.Color = _quadData.SquareColor;
			sprite3.Material = new Material("Standard2D");
			sprite3.SpriteData = RenderData2D.QuadData;
			//sprite3.Texture = AssetManager.LoadTexture2D("assets/textures/sampleDog.png", TextureUsage.Sampled);

			for (int i = 4; i < _quadData.QuadCount; i++)
			{
				var quad5 = ActiveScene.CreateEntity(quad4);
				ref var transform5 = ref quad5.GetComponent<TransformComponent>();
				transform5.Translation = new Vector3(-.11f * (i % 1000), -.11f * (i / 1000), .5f);
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

			ActiveScene.Update();
		}

		public override void OnGuiRender()
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			ImGui.Begin("Settings 2D");
			ImGui.ColorEdit4("Square Color 2D", ref _quadData.SquareColor);
			ImGui.End();

			ImGui.Begin("FPS Counter");
			ImGui.PlotLines("", ref _frametime[0], 100, 0, "Frametime (ms)", 0, 66.6f, new Vector2(250, 50));
			ImGui.Text("FPS: " + (1000f / _frametime.Average()).ToString());
			ImGui.End();

			ImGui.Begin("QuadDraw Config");
			ImGui.DragInt("QuadCount", ref _quadData.QuadCount, 1, 0, 3000000);
			ImGui.End();
		}

		public override void OnEvent(EventBase @event)
		{
			if (@event is WindowResizeEvent)
			{
				var ev = @event as WindowResizeEvent;
				ActiveScene.CreateEmptyEntity().AddComponent(new ViewportResizedEvent()
				{
					Height = ev.Height,
					Width = ev.Width
				});
			}
		}
		public void LoadStandardShaders()
		{
			#region Standard2D
			var standard2DShaders = AssetManager.GenerateShadersFromFile("TexturedInstancing.glsl", "Standard2D");
			var standard2DResourceLayout = ResourceFactory.CreateResourceLayout(
									new ResourceLayoutDescription(
										new ResourceLayoutElementDescription("ViewProjection", ResourceKind.UniformBuffer, ShaderStages.Vertex),
										new ResourceLayoutElementDescription("Texture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
										new ResourceLayoutElementDescription("Sampler", ResourceKind.Sampler, ShaderStages.Fragment)));

			Renderer2D.Instance.AddShader("Standard2D", new ShaderData()
			{
				Shaders = standard2DShaders,
				ResourceLayouts = new ResourceLayout[] { standard2DResourceLayout }
			});
			#endregion
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
