using BootEngine.AssetsManager;
using BootEngine.Audio;
using BootEngine.ECS.Components;
using BootEngine.ECS.Components.Events;
using BootEngine.ECS.Systems;
using BootEngine.Layers;
using BootEngine.Layers.GUI;
using BootEngine.Renderer;
using BootEngine.Scripting;
using BootEngine.Serializers;
using BootEngine.Utils;
using BootEngine.Utils.ProfilingTools;
using BootEngine.Window;
using ImGuiNET;
using Leopotam.Ecs;
using Shoelace.assets.scripts;
using Shoelace.Panels;
using Shoelace.Serializers;
using Shoelace.Services;
using Shoelace.Styling;
using Shoelace.Systems;
using System;
using System.IO;
using System.Linq;
using System.Numerics;
using Veldrid;

namespace Shoelace.Layers
{
	public sealed class EditorLayer : LayerBase
	{
		#region Properties
		private IntPtr renderTargetAddr;
		private bool dockspaceOpen = true;
		private Vector2 lastSize = Vector2.Zero;
		private readonly float[] _frametime = new float[100];
		private SceneHierarchyPanel sceneHierarchyPanel = new SceneHierarchyPanel();
		private PropertiesPanel propertiesPanel = new PropertiesPanel();
		private AssetManagerPanel assetManagerPanel = null;
		private bool runtimeActive;
		private readonly GizmoSystem _guizmoSystem = new GizmoSystem();
		private readonly GuiService _guiService = new GuiService();
		#endregion

		#region Constructor
		public EditorLayer() : base("Editor") { }
		#endregion

		public override void OnAttach()
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			LoadFonts();

			var loadedSound = AssetManager.LoadSound("assets\\sounds\\loaded.mp3", false); //sample sound loading
			Styles.SetDarkTheme();

			EditorHelper.LoadStandardShaders();

			LoadScene();

			LoadDemoScene();

			SoundEngine.Instance.PlaySound(loadedSound);
		}

		private void LoadDemoScene()
		{
			LoadScene(Path.Combine(EditorConfig.AssetDirectory, "scenes\\VerySimpleMemoryGame.boot"));

			var filter = ActiveScene.GetFilter(typeof(EcsFilter<TransformComponent, SpriteRendererComponent>));
			int i = 0;
			System.Collections.Generic.List<RevealOnClick> scriptList = new System.Collections.Generic.List<RevealOnClick>();
			foreach (var card in filter)
			{
				EcsEntity entt = filter.GetEntity(card);
				ref var script = ref entt.Get<ScriptingComponent>();
				script.Script = new RevealOnClick(new BootEngine.ECS.Entity(entt), KeyCodes.Number1 + i);
				scriptList.Add((RevealOnClick)script.Script);
				i++;
			}
			foreach (var card in filter)
			{
				EcsEntity entt = filter.GetEntity(card);
				ref var script = ref entt.Get<ScriptingComponent>();
				((RevealOnClick)script.Script).Entities = scriptList;
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
			#region Dockspace Setup
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
			style.WindowMinSize = new Vector2(330.0f, style.WindowMinSize.Y);

			ImGui.DockSpace(ImGui.GetID("MainDockspace"), Vector2.Zero);
			style.WindowMinSize = minWinSize;
			#endregion

			#region File Dialogs
			if (FileDialog.ShowFileDialog(ref _guiService.ShouldLoadScene, out string loadPath, FileDialog.DialogType.Open, "Boot Scene", ".boot"))
			{
				LoadScene(loadPath);
				_guiService.ShouldLoadScene = false;
			}

			if (FileDialog.ShowFileDialog(ref _guiService.ShouldSaveScene, out string savePath, FileDialog.DialogType.Save))
			{
				SaveScene(savePath);
				_guiService.ShouldSaveScene = false;
			}

			if (_guiService.NewScene)
			{
				LoadScene();
			}
			#endregion

			#region MainMenuBar
			if (ImGui.BeginMenuBar())
			{
				if (ImGui.BeginMenu("File"))
				{
					if (ImGui.MenuItem(FontAwesome5.FileMedical + " New Scene", "Ctrl+N"))
					{
						LoadScene();
					}

					if (ImGui.MenuItem(FontAwesome5.FolderOpen + " Open Scene...", "Ctrl+O"))
					{
						_guiService.ShouldLoadScene = true;
					}

					if (ImGui.MenuItem(FontAwesome5.Save + " Save Scene", "Ctrl+S"))
					{
						SaveScene($"assets/scenes/{ActiveScene.Title}.boot");
					}

					if (ImGui.MenuItem(FontAwesome5.Save + " Save Scene As...", "Ctrl+Shift+S"))
					{
						_guiService.ShouldSaveScene = true;
					}

					if (ImGui.MenuItem(FontAwesome5.Times + " Exit", "Ctrl+Q"))
					{
						Close();
					}

					ImGui.EndMenu();
				}

				// TODO: Implement a proper "play" button
				if (ImGui.BeginMenu("Runtime"))
				{
					if (ImGui.Checkbox("", ref runtimeActive))
					{
						ActiveScene.EnableRuntimeSystems(runtimeActive);
					}
					ImGui.SameLine();
					if (ImGui.MenuItem("Enabled"))
					{
						runtimeActive = !runtimeActive;
						ActiveScene.EnableRuntimeSystems(runtimeActive);
					}
					ImGui.EndMenu();
				}
				ImGui.EndMenuBar();
			}
			#endregion

			#region Debug Data
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
			#endregion

			sceneHierarchyPanel.OnGuiRender();
			propertiesPanel.OnGuiRender();
			assetManagerPanel.OnGuiRender();

			#region Scene Viewport
			ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
			ImGui.Begin(FontAwesome5.Gamepad + " Viewport");
			_guiService.ViewportFocused = ImGui.IsWindowFocused();
			_guiService.ViewportHovered = ImGui.IsWindowHovered();
			_guiService.BlockEvents = !_guiService.ViewportFocused || !_guiService.ViewportHovered;

			var viewportPanelSize = ImGui.GetContentRegionAvail();
			if (!GraphicsDevice.IsUvOriginTopLeft)
				ImGui.Image(renderTargetAddr, viewportPanelSize, new Vector2(0, 1), new Vector2(1, 0));
			else
				ImGui.Image(renderTargetAddr, viewportPanelSize);

			if (viewportPanelSize != lastSize)
			{
				lastSize = viewportPanelSize;
				ActiveScene.CreateEmptyEntity().AddComponent(new ViewportResizedEvent()
				{
					Width = (int)lastSize.X,
					Height = (int)lastSize.Y
				});
			}
			#endregion

			_guizmoSystem.ProcessGizmos();

			ImGui.End(); //Viewport
			ImGui.PopStyleVar();

			ImGui.End(); // Dockspace
		}

		private void SaveScene(string savePath)
		{
			new YamlSerializer()
				.WithCustomSerializer(new EditorCameraSerializer(ActiveScene))
				.Serialize(savePath, ActiveScene);
		}

		private void LoadScene(string path = null)
		{
			ActiveScene = new BootEngine.ECS.Scene();
			sceneHierarchyPanel = new SceneHierarchyPanel();
			propertiesPanel = new PropertiesPanel();
			assetManagerPanel = new AssetManagerPanel();
			_guiService.SelectedEntity = default;
			ActiveScene
				.AddSystem(assetManagerPanel)
				.AddSystem(sceneHierarchyPanel)
				.AddSystem(propertiesPanel)
				.AddSystem(_guizmoSystem)
				.AddSystem(new GuiControlSystem(), "GUI Control System")
				.AddSystem(new EditorCameraSystem(), "Editor Camera")
				.AddRuntimeSystem(new VelocitySystem(), "Velocity System")
				.AddRuntimeSystem(new ScriptingSystem(), "Scripting System")
				.Inject(_guiService)
				.Init();

			renderTargetAddr = EditorHelper.CreateEditorCamera(Width, Height, ActiveScene);

			if (path != null)
			{
				ActiveScene = new SceneDeserializer()
					.WithCustomDeserializer(new EditorCameraDeserializer())
					.Deserialize(path, ActiveScene);
			}

			ActiveScene.CreateEmptyEntity().AddComponent(new ViewportResizedEvent()
			{
				Width = (int)lastSize.X,
				Height = (int)lastSize.Y
			});

			_guiService.NewScene = false;
		}

		private static void LoadFonts()
		{
			ImGuiFontInfo[] fonts = new ImGuiFontInfo[]
			{
				new ImGuiFontInfo()
				{
					IsIconFont = false,
					MergeMode = false,
					Path = Path.Combine(AppContext.BaseDirectory, "internalAssets/fonts/WorkSans/static/WorkSans-Regular.ttf"),
					Size = 14f
				},
				new ImGuiFontInfo()
				{
					IsIconFont = true,
					MergeMode = true,
					Path = Path.Combine(AppContext.BaseDirectory, "internalAssets/fonts/fontawesome-free-5.15.1-web/webfonts/fa-solid-900.ttf"),
					Size = 14f,
					Ranges = new ushort[] { 0xe005, 0xf8ff, 0 }
				},
				new ImGuiFontInfo()
				{
					IsIconFont = true,
					MergeMode = true,
					Path = Path.Combine(AppContext.BaseDirectory, "internalAssets/fonts/fontawesome-free-5.15.1-web/webfonts/fa-regular-400.ttf"),
					Size = 14f,
					Ranges = new ushort[] { 0xe005, 0xf8ff, 0 }
				},
				new ImGuiFontInfo()
				{
					IsIconFont = false,
					MergeMode = false,
					Path = Path.Combine(AppContext.BaseDirectory, "internalAssets/fonts/WorkSans/static/WorkSans-SemiBold.ttf"),
					Size = 14f
				}
			};
			ImGuiLayer.LoadFonts(fonts);
		}

		public override void OnDetach()
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			Renderer2D.Instance.Dispose();
			SoundEngine.Instance.Dispose();
			EditorHelper.FreeResources();
		}
	}
}