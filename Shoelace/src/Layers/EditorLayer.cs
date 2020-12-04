using BootEngine.AssetsManager;
using BootEngine.ECS.Components;
using BootEngine.ECS.Components.Events;
using BootEngine.ECS.Systems;
using BootEngine.Layers;
using BootEngine.Layers.GUI;
using BootEngine.Renderer;
using BootEngine.Renderer.Cameras;
using BootEngine.Utils;
using BootEngine.Utils.ProfilingTools;
using ImGuiNET;
using Shoelace.Panels;
using Shoelace.Services;
using Shoelace.Systems;
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
		private Texture fbTex, fbDepthTex;
		private IntPtr renderTargetAddr;
		private Framebuffer fb;
		private bool dockspaceOpen = true;
		private bool systemManagerEnabled = false;
		private Vector2 lastSize = Vector2.Zero;
		private readonly float[] _frametime = new float[100];
		private readonly GuiService _guiService = new GuiService();
		private readonly SceneHierarchyPanel _sceneHierarchyPanel = new SceneHierarchyPanel();
		private readonly PropertiesPanel _propertiesPanel = new PropertiesPanel();
		private readonly GuizmoSystem _guizmoSystem = new GuizmoSystem();
		#endregion

		#region Constructor
		public EditorLayer() : base("Editor") { }
		#endregion

		public override void OnAttach()
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			ActiveScene
				.AddSystem(new GuiControlSystem(), "GUI Control System")
				.AddSystem(new EditorCameraSystem(), "Editor Camera")
				.AddSystem(_sceneHierarchyPanel)
				.AddSystem(_propertiesPanel)
				.AddSystem(_guizmoSystem)
				.AddRuntimeSystem(new VelocitySystem(), "Velocity System")
				.Inject(_guiService)
				.Init();

			Styles.SetDarkTheme();

			//TODO: Main Camera should be a "special" entity
			var editorCam = ActiveScene.CreateEntity("Main Camera");
			var camera = new OrthoCamera(1, -1, 1, Width, Height);
			camera.SetPerspective(MathUtil.Deg2Rad(70), .0001f, 1000f);
			editorCam.AddComponent(new CameraComponent()
			{
				Camera = camera
			});
			//editorCam.AddComponent<TransformComponent>();

			var redQuad = ActiveScene.CreateEntity("Red Textured Quad");
			ref var sprite = ref redQuad.AddComponent<SpriteComponent>();
			redQuad.ReplaceComponent(new TransformComponent()
			{
				Position = new Vector3(.1f, .1f, -2.5f)
			});
			sprite.Color = ColorF.HoverRed;
			sprite.Texture = AssetManager.LoadTexture2D("assets/textures/sampleBoot.png", BootEngine.Utils.TextureUsage.Sampled);

			var pinkQuad = ActiveScene.CreateEntity("Pink Quad").AddComponent(new SpriteComponent(squareColor));
			ref var transform = ref pinkQuad.GetComponent<TransformComponent>();
			transform.Position = new Vector3(0f, 0f, -3.5f);
			transform.Scale = new Vector3(1f, 1f, .5f);
			transform.Rotation = new Vector3(0,0, MathUtil.Deg2Rad(45));
			pinkQuad.AddComponent(new VelocityComponent()
			{
				//RotationSpeed = new Vector3(0, 0, MathUtil.Deg2Rad(10))
			});

			var currentPipeline = Renderer2D.Instance.PipelineDescrition;
			fbTex = ResourceFactory.CreateTexture(TextureDescription.Texture2D(
				(uint)Width, // Width
				(uint)Height, // Height
				1,  // Miplevel
				1,  // ArrayLayers
				PixelFormat.R8_G8_B8_A8_UNorm,
				Veldrid.TextureUsage.RenderTarget | Veldrid.TextureUsage.Sampled));
			renderTargetAddr = ImGuiLayer.GetOrCreateImGuiBinding(ResourceFactory, fbTex);

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
			_frametime[99] = deltaSeconds * 1000;
			for (int i = 0; i < 99;)
			{
				_frametime[i] = _frametime[++i];
			}

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

			var style = ImGui.GetStyle();
			Vector2 minWinSize = style.WindowMinSize;
			style.WindowMinSize = new Vector2(300.0f, style.WindowMinSize.Y);

			ImGui.DockSpace(ImGui.GetID("MyDockspace"), Vector2.Zero);
			style.WindowMinSize = minWinSize;

			if (ImGui.BeginMenuBar())
			{
				if (ImGui.BeginMenu("File"))
				{
					if (ImGui.MenuItem("Exit", "Ctrl+Q"))
						Close();
					ImGui.EndMenu();
				}

				if (ImGui.BeginMenu("Windows"))
				{
					if (ImGui.MenuItem("System Manager"))
						systemManagerEnabled = !systemManagerEnabled;
					ImGui.EndMenu();
				}

				ImGui.EndMenuBar();
			}

			ImGui.Begin("Stats");
			ImGui.Text("Renderer Stats:");
			ImGui.Text("Draw Calls: " + Renderer2D.Instance.Stats.DrawCalls.ToString());
			ImGui.Text("Instance Count: " + Renderer2D.Instance.Stats.InstanceCount.ToString());
			Renderer2D.Instance.ResetStats();

			ImGui.Separator();

			ImGui.PlotLines("", ref _frametime[0], 100, 0, "Frametime (ms)", 0, 66.6f, new Vector2(250, 50));
			ImGui.Text("FPS: " + (1000f / _frametime.Average()).ToString());
			ImGui.Text("Frametime: " + _frametime.Average().ToString());

			ImGui.Separator();

			ImGui.End(); // Stats

			_sceneHierarchyPanel.OnGuiRender();
			_propertiesPanel.OnGuiRender();

			ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
			ImGui.Begin("Viewport");
			_guiService.ViewportFocused = ImGui.IsWindowFocused();
			_guiService.ViewportHovered = ImGui.IsWindowHovered();
			_guiService.BlockEvents = !_guiService.ViewportFocused || !ImGui.IsWindowHovered();

			var viewportPanelSize = ImGui.GetContentRegionAvail();
			if (!GraphicsDevice.IsUvOriginTopLeft)
				ImGui.Image(renderTargetAddr, viewportPanelSize, new Vector2(0,1), new Vector2(1,0));
			else
				ImGui.Image(renderTargetAddr, viewportPanelSize);

			if (viewportPanelSize != lastSize)
			{
				lastSize = viewportPanelSize;
				ActiveScene.CreateEmptyEntity().AddComponent(new ViewportResizedEvent()
				{
					Width = (int)viewportPanelSize.X,
					Height = (int)viewportPanelSize.Y
				});
			}

			_guizmoSystem.ProcessGizmos();

			ImGui.End(); //Viewport
			ImGui.PopStyleVar();

			if (systemManagerEnabled)
			{
				//ImGui.Begin("System Manager");
				//// TODO: Add system controls
				//ImGui.End();
			}

			ImGui.End(); // Dockspace
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