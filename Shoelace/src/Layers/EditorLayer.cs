using BootEngine.AssetsManager;
using BootEngine.ECS;
using BootEngine.ECS.Components;
using BootEngine.ECS.Components.Events;
using BootEngine.ECS.Systems;
using BootEngine.Events;
using BootEngine.Input;
using BootEngine.Layers;
using BootEngine.Layers.GUI;
using BootEngine.Renderer;
using BootEngine.Renderer.Cameras;
using BootEngine.Utils;
using BootEngine.Utils.ProfilingTools;
using ImGuiNET;
using System;
using System.Linq;
using System.Numerics;
using Veldrid;

namespace Shoelace.Layers
{
	public sealed class EditorLayer : LayerBase
	{
		#region Properties
		private Vector4 squareColor = ColorF.Pink;
		private readonly float[] _frametime = new float[100];
		private Texture fbTex, fbDepthTex;
		private IntPtr renderTargetAddr;
		private Framebuffer fb;
		private bool dockspaceOpen = true;
		private Vector2 lastSize = Vector2.Zero;
		private bool viewportFocused;
		private Scene ActiveScene;
		#endregion

		#region Constructor
		public EditorLayer() : base("Editor") { }
		#endregion

		public override void OnAttach()
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			ActiveScene = new Scene();
			ActiveScene.AddSystem(new CameraSystem(), "Camera System")
				.AddSystem(new RenderSystem())
				.Init();

			var cam = ActiveScene.CreateEntity("Main Camera");
			var camera = new OrthoCamera(1, -1, 1);
			camera.ResizeViewport(Width, Height);
			camera.FixedAspectRatio = false;
			cam.AddComponent(new CameraComponent()
			{
				Camera = camera
			});
			cam.AddComponent<CameraMovementComponent>();

			var redQuad = ActiveScene.CreateEntity("Red Textured Quad");
			ref var sprite = ref redQuad.AddComponent<SpriteComponent>();
			sprite.Color = ColorF.Red;
			sprite.Texture = AssetManager.LoadTexture2D("assets/textures/sampleFly.png", BootEngine.Utils.TextureUsage.Sampled);

			var pinkQuad = ActiveScene.CreateEntity("Pink Quad").AddComponent(new SpriteComponent(squareColor));
			ref var transform = ref pinkQuad.GetComponent<TransformComponent>();
			transform.Position = new Vector3(0, 0, -.5f);
			transform.Scale = new Vector3(.5f, .5f, .5f);
			transform.Rotation = new Vector3(0, 0, 45);

			var currentPipeline = Renderer2D.Instance.PipelineDescrition;
			fbTex = ResourceFactory.CreateTexture(TextureDescription.Texture2D(
				(uint)Width, // Width
				(uint)Height, // Height
				1,  // Miplevel
				1,  // ArrayLayers
				PixelFormat.R8_G8_B8_A8_UNorm,
				Veldrid.TextureUsage.RenderTarget | Veldrid.TextureUsage.Sampled));
			renderTargetAddr = ImGuiLayer.Controller.GetOrCreateImGuiBinding(ResourceFactory, fbTex);

			fbDepthTex = ResourceFactory.CreateTexture(TextureDescription.Texture2D(
				(uint)Width, // Width
				(uint)Height, // Height
				1,  // Miplevel
				1,  // ArrayLayers
				PixelFormat.R16_UNorm,
				Veldrid.TextureUsage.DepthStencil));
			fb = ResourceFactory.CreateFramebuffer(new FramebufferDescription(fbDepthTex, fbTex));
			currentPipeline.Outputs = fb.OutputDescription;
			Renderer2D.Instance.SetPipelineDescrition(currentPipeline, fb, true);
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

			//if (viewportFocused)
			//	cameraController.Update(deltaSeconds);

#if DEBUG
			using (_ = new Profiler("Scene Update"))
			{
#endif
				ActiveScene.Update();
#if DEBUG
			}
#endif
		}

		public override void OnGuiRender()
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			var viewport = ImGui.GetMainViewport();
			ImGui.SetNextWindowPos(viewport.Pos);
			ImGui.SetNextWindowSize(viewport.Size);
			ImGui.SetNextWindowViewport(viewport.ID);
			ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0.0f);
			ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0.0f);
			const ImGuiWindowFlags dockspaceFlags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize
				| ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoNavFocus | ImGuiWindowFlags.NoBackground
				| ImGuiWindowFlags.MenuBar;
			ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
			ImGui.Begin("Dockspace", ref dockspaceOpen, dockspaceFlags);
			ImGui.PopStyleVar(3);
			ImGui.DockSpace(ImGui.GetID("MyDockspace"), Vector2.Zero);
			if (ImGui.BeginMenuBar())
			{
				if (ImGui.BeginMenu("File"))
				{
					if (ImGui.MenuItem("Exit", "Ctrl+Q"))
						Close();
					ImGui.EndMenu();
				}
				ImGui.EndMenuBar();
			}

			ImGui.Begin("Settings 2D");
			ImGui.ColorEdit4("Square Color 2D", ref squareColor);

			ImGui.Begin("Stats");
			ImGui.Text("Renderer Stats:");
			ImGui.Text("Draw Calls: " + Renderer2D.Instance.Stats.DrawCalls.ToString());
			ImGui.Text("Instance Count: " + Renderer2D.Instance.InstanceCount.ToString());

			ImGui.Separator();

			ImGui.PlotLines("", ref _frametime[0], 100, 0, "Frametime (ms)", 0, 66.6f, new Vector2(250, 50));
			ImGui.Text("FPS: " + (1000f / _frametime.Average()).ToString());

			ImGui.Separator();

			ImGui.End(); // Stats
			ImGui.End(); // Settings 2D

			ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
			ImGui.Begin("Viewport");
			viewportFocused = ImGui.IsWindowFocused();
			ImGuiLayer.BlockEvents = !viewportFocused || !ImGui.IsWindowHovered();

			var viewportPanelSize = ImGui.GetContentRegionAvail();
			if (!GraphicsDevice.IsUvOriginTopLeft)
				ImGui.Image(renderTargetAddr, viewportPanelSize, new Vector2(0,1), new Vector2(1,0));
			else
				ImGui.Image(renderTargetAddr, viewportPanelSize);
			if (viewportPanelSize != lastSize)
			{
				lastSize = viewportPanelSize;
				//cameraController.OnResize((int)viewportPanelSize.X, (int)viewportPanelSize.Y);
				ActiveScene.CreateEntity().AddComponent(new ViewportResizedEvent()
				{
					Width = (int)viewportPanelSize.X,
					Height = (int)viewportPanelSize.Y
				});
			}
			ImGui.End(); //Viewport
			ImGui.PopStyleVar();
			ImGui.End(); // Dockspace
		}

		public override void OnEvent(EventBase @event)
		{
			//cameraController.OnEvent(@event);
			EventDispatcher dis = new EventDispatcher(@event);
			dis.Dispatch<KeyPressedEvent>(OnKeyPressed);
		}

		private bool OnKeyPressed(KeyPressedEvent e)
		{
			bool control = InputManager.Instance.GetKeyDown(KeyCodes.ControlLeft) || InputManager.Instance.GetKeyDown(KeyCodes.ControlRight);
			bool shift = InputManager.Instance.GetKeyDown(KeyCodes.ShiftLeft) || InputManager.Instance.GetKeyDown(KeyCodes.ShiftRight);

			switch (e.KeyCode)
			{
				case KeyCodes.N:
				case KeyCodes.O:
				case KeyCodes.S:
					break;
				case KeyCodes.Q:
					if (control)
						Close();
					break;
			}
			return false;
		}

		public override void OnDetach()
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			fb.Dispose();
			fbTex.Dispose();
			fbDepthTex.Dispose();
			Renderer2D.Instance.Dispose();
		}
	}
}