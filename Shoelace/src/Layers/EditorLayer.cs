using BootEngine;
using BootEngine.Events;
using BootEngine.Layers;
using BootEngine.Renderer;
using BootEngine.Renderer.Cameras;
using BootEngine.Utils;
using BootEngine.Utils.ProfilingTools;
using ImGuiNET;
using System;
using System.Numerics;
using System.Threading.Tasks;
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
		private bool dockspaceOpen = true;
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
			Renderer2D.CurrentScene.RenderToFramebuffer = true;
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
			//ImGui.Begin("Settings 2D");
			//ImGui.ColorEdit4("Square Color 2D", ref squareColor);
			//ImGui.End();

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
			if (ImGui.BeginMenuBar())
			{
				if (ImGui.BeginMenu("File"))
				{
					if (ImGui.MenuItem("Exit"))
						Close();
					ImGui.EndMenu();
				}
				ImGui.EndMenuBar();
			}

			ImGui.Begin("Viewport");
			var focused = ImGui.IsWindowFocused();
			var hovered = ImGui.IsWindowHovered();

			var viewportPanelSize = ImGui.GetContentRegionAvail();
			if (!GraphicsDevice.IsUvOriginTopLeft)
				ImGui.Image(Renderer2D.RenderTargetAddr, viewportPanelSize, new Vector2(0,1), new Vector2(1,0));
			else
				ImGui.Image(Renderer2D.RenderTargetAddr, viewportPanelSize);
			ImGui.End(); //Viewport
			ImGui.End(); // Dockspace
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