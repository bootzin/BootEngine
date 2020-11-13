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
using System.Numerics;
using System.Threading.Tasks;
using Utils;
using Veldrid;

namespace Shoelace.Layers
{
	public class EditorLayer : LayerBase
	{
		#region Properties
		private OrthoCameraController cameraController;
		private Vector4 squareColor = ColorF.Pink;
		private float rot;
		private int instanceCount = 1;
		private readonly float[] _frametime = new float[100];
		private Texture fbTex, fbDepthTex;
		private IntPtr renderTargetAddr;
		private Framebuffer fb;
		private bool dockspaceOpen = true;
		private Vector2 lastSize = Vector2.Zero;
		private bool viewportFocused;
		#endregion

		#region Constructor
		public EditorLayer() : base("Shoelace") { }
		#endregion

		public override void OnAttach()
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			float aspectRatio = (float)Width / Height;
			cameraController = new OrthoCameraController(aspectRatio);

			Renderable2DParameters param = new Renderable2DParameters
			{
				Name = "Quad",
				Position = new Vector3(-1, 0, .5f),
				Size = new Vector2(.5f, .5f),
				Rotation = 0,
				Color = squareColor,
				Texture = null
			};
			Renderer2D.Instance.SetupQuadDraw(ref param);

			var currentPipeline = Renderer2D.CurrentScene.PipelineDescrition;
			fbTex = ResourceFactory.CreateTexture(TextureDescription.Texture2D(
				1264u, // Width
				714u, // Height
				1,  // Miplevel
				1,  // ArrayLayers
				PixelFormat.R8_G8_B8_A8_UNorm,
				Veldrid.TextureUsage.RenderTarget | Veldrid.TextureUsage.Sampled));
			renderTargetAddr = ImGuiLayer.Controller.GetOrCreateImGuiBinding(ResourceFactory, fbTex);

			fbDepthTex = ResourceFactory.CreateTexture(TextureDescription.Texture2D(
				1264u, // Width
				714u, // Height
				1,  // Miplevel
				1,  // ArrayLayers
				PixelFormat.R16_UNorm,
				Veldrid.TextureUsage.DepthStencil));
			fb = ResourceFactory.CreateFramebuffer(new FramebufferDescription(fbDepthTex, fbTex));
			currentPipeline.Outputs = fb.OutputDescription;
			Renderer2D.CurrentScene.SetPipelineDescrition(currentPipeline, fb);
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

			if (viewportFocused)
				cameraController.Update(deltaSeconds);
			Renderer2D renderer = Renderer2D.Instance;
			renderer.BeginScene(cameraController.Camera);
			renderer.UpdatePosition("Quad", new Vector3(0, 0, .9f));

#if DEBUG
			using (Profiler updateProfiler = new Profiler("Update"))
#endif
				Parallel.For(0, renderer.InstanceCount, (i) =>
				{
					renderer.UpdateColor(i, squareColor);
					renderer.UpdateRotation(i, Utils.Util.Deg2Rad(rot));
				});
			rot++;

			if (renderer.InstanceCount < instanceCount)
			{
				var param = new Renderable2DParameters
				{
					Size = new Vector2(.1f, .1f),
					Rotation = 0,
					Color = squareColor
				};
				for (int i = renderer.InstanceCount; i < instanceCount; i++)
				{
					param.Position = new Vector3(-.11f * (i % 1000), -.11f * (i / 1000), .5f);
					renderer.SetupQuadDraw(ref param);
				}
			}
			else if (renderer.InstanceCount > instanceCount)
			{
				for (int i = renderer.InstanceCount; i > instanceCount;)
					renderer.RemoveQuadDraw(--i);
			}

#if DEBUG
			using (Profiler camProfiler = new Profiler("Rendering"))
			{
#endif
				renderer.Render();
#if DEBUG
			}
#endif
			renderer.EndScene();

			renderer.BeginScene(cameraController.Camera, false);
			renderer.UpdatePosition("Quad", new Vector3(squareColor.X, squareColor.Y, squareColor.Z));
			renderer.Render();
			renderer.EndScene();
		}

		public override void OnGuiRender()
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			//ImGui.Begin("FPS Counter");
			//ImGui.PlotLines("", ref _frametime[0], 100, 0, "Frametime (ms)", 0, 66.6f, new Vector2(250, 50));
			//ImGui.Text("FPS: " + (1000f / _frametime.Average()));
			//ImGui.End();

			//ImGui.Begin("QuadDraw Config");
			//ImGui.DragInt("QuadCount", ref instanceCount, 1, 0, 3000000);
			//ImGui.End();

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
			ImGui.End();

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
				cameraController.OnResize((int)viewportPanelSize.X, (int)viewportPanelSize.Y);
			}
			ImGui.End(); //Viewport
			ImGui.PopStyleVar();
			ImGui.End(); // Dockspace
		}

		public override void OnEvent(EventBase @event)
		{
			cameraController.OnEvent(@event);
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