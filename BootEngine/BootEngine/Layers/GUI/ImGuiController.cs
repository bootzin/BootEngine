﻿using BootEngine.Input;
using BootEngine.Renderer.Cameras;
using BootEngine.Utils.ProfilingTools;
using BootEngine.Window;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BootEngine.Utils;
using BootEngine.Utils.Exceptions;
using BootEngine.Utils.Unsafe;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.Vk;

namespace BootEngine.Layers.GUI
{
	internal sealed class ImGuiController : IDisposable
	{
		#region Properties
		private GraphicsDevice graphicsDevice;
		private bool frameBegun;

		// Veldrid objects
		private DeviceBuffer vertexBuffer;
		private DeviceBuffer indexBuffer;
		private DeviceBuffer projMatrixBuffer;
		private Texture fontTexture;
		private TextureView fontTextureView;
		private Shader vertexShader;
		private Shader fragmentShader;
		private ResourceLayout layout;
		private ResourceLayout textureLayout;
		private Pipeline pipeline;
		private ResourceSet mainResourceSet;
		private ResourceSet fontTextureResourceSet;
		private readonly WindowBase mainWindow;

		private readonly IntPtr fontAtlasID = (IntPtr)1;

		private int windowWidth;
		private int windowHeight;
		private Vector2 scaleFactor = Vector2.One;

		#region Delegates
		private delegate void Platform_CreateWindow(ImGuiViewportPtr viewport);
		private delegate void Platform_DestroyWindow(ImGuiViewportPtr viewport);
		private delegate void Platform_ShowWindow(ImGuiViewportPtr viewport);
		private delegate void Platform_GetWindowPosition(Vector2 pos, ImGuiViewportPtr viewport);
		private delegate void Platform_GetWindowSize(Vector2 size, ImGuiViewportPtr viewport);
		private delegate void Platform_SetWindowPosition(ImGuiViewportPtr viewport, Vector2 pos);
		private delegate void Platform_SetWindowSize(ImGuiViewportPtr viewport, Vector2 size);
		private delegate void Platform_SetWindowTitle(ImGuiViewportPtr viewport, string title);
		private delegate bool Platform_GetWindowFocus(ImGuiViewportPtr viewport);
		private delegate byte Platform_GetWindowMinimized(ImGuiViewportPtr viewport);
		private delegate void Platform_SetWindowAlpha(ImGuiViewportPtr viewport, float alpha);

		private delegate bool Platform_CreateVkSurface(ImGuiViewportPtr viewport);

#pragma warning disable S1450 // Private fields only used as local variables in methods should become local variables
		private Platform_CreateWindow createWindow;
		private Platform_DestroyWindow destroyWindow;
		private Platform_ShowWindow showWindow;
		private Platform_GetWindowPosition getWindowPosition;
		private Platform_SetWindowPosition setWindowPosition;
		private Platform_GetWindowSize getWindowSize;
		private Platform_SetWindowSize setWindowSize;
		private Platform_SetWindowTitle setWindowTitle;
		private Platform_GetWindowFocus getWindowFocus;
		private Platform_SetWindowFocus setWindowFocus;
		private Platform_SetWindowAlpha setWindowAlpha;
		private Platform_GetWindowMinimized getWindowMinimized;

		private Platform_CreateVkSurface createVulkanSurface;
#pragma warning restore S1450 // Private fields only used as local variables in methods should become local variables

		#region SDL
		private delegate int SDL_GetNumVideoDisplays_t();
		private static SDL_GetNumVideoDisplays_t p_sdl_GetNumVideoDisplays;

		private delegate void SDL_RaiseWindow_t(IntPtr sdl2Window);
		private static SDL_RaiseWindow_t p_sdl_RaiseWindow;

		private unsafe delegate uint SDL_GetGlobalMouseState_t(int* x, int* y);
		private static SDL_GetGlobalMouseState_t p_sdl_GetGlobalMouseState;

		private unsafe delegate int SDL_GetDisplayUsableBounds_t(int displayIndex, Rectangle* rect);
		private static SDL_GetDisplayUsableBounds_t p_sdl_GetDisplayUsableBounds_t;

		private static SDL_GetNumVideoDisplays_t Sdl_GetNumVideoDisplays
		{
			get
			{
				return p_sdl_GetNumVideoDisplays ??= Sdl2Native.LoadFunction<SDL_GetNumVideoDisplays_t>("SDL_GetNumVideoDisplays");
			}
		}

		private static SDL_RaiseWindow_t Sdl_RaiseWindow
		{
			get
			{
				return p_sdl_RaiseWindow ??= Sdl2Native.LoadFunction<SDL_RaiseWindow_t>("SDL_RaiseWindow");
			}
		}

		private static SDL_GetGlobalMouseState_t Sdl_GetGlobalMouseState
		{
			get
			{
				return p_sdl_GetGlobalMouseState ??= Sdl2Native.LoadFunction<SDL_GetGlobalMouseState_t>("SDL_GetGlobalMouseState");
			}
		}

		private static SDL_GetDisplayUsableBounds_t Sdl_GetDisplayUsableBounds
		{
			get
			{
				return p_sdl_GetDisplayUsableBounds_t ??= Sdl2Native.LoadFunction<SDL_GetDisplayUsableBounds_t>("SDL_GetDisplayUsableBounds");
			}
		}
		#endregion
		#endregion

		// Image trackers
		private readonly Dictionary<TextureView, ResourceSetInfo> setsByView = new Dictionary<TextureView, ResourceSetInfo>();
		private readonly Dictionary<Texture, TextureView> autoViewsByTexture = new Dictionary<Texture, TextureView>();
		private readonly Dictionary<IntPtr, ResourceSetInfo> viewsById = new Dictionary<IntPtr, ResourceSetInfo>();
		private readonly List<IDisposable> ownedResources = new List<IDisposable>();
		private int lastAssignedID = 100;
		#endregion

		/// <summary>
		/// Constructs a new ImGuiController.
		/// </summary>
		public ImGuiController(GraphicsDevice gd, OutputDescription outputDescription, WindowBase window)
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			mainWindow = window;
			Sdl2Window sdlWindow = window.SdlWindow;

			windowWidth = sdlWindow.Width;
			windowHeight = sdlWindow.Height;

			ImPlot.CreateContext();

			ImNodes.Initialize();

			IntPtr imguiCtx = ImGui.CreateContext();
			ImGui.SetCurrentContext(imguiCtx);
			ImPlot.SetImGuiContext(imguiCtx);
			ImNodes.SetImGuiContext(imguiCtx);
			ImGuizmo.SetImGuiContext(imguiCtx);

			ImGui.StyleColorsDark();

			CreateDeviceResources(gd, outputDescription);

			SetupImGuiIo(sdlWindow);
			SetImGuiKeyMappings();

			SetPerFrameImGuiData(1f / 60);
		}

		public void WindowResized(int width, int height, bool minimized)
		{
			if (!minimized)
			{
				windowWidth = width;
				windowHeight = height;
			}
			else
			{
				windowWidth = 0;
				windowHeight = 0;
			}
		}

		public void CreateDeviceResources(GraphicsDevice gd, OutputDescription outputDescription)
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			graphicsDevice = gd;

			vertexBuffer = gd.ResourceFactory.CreateBuffer(new BufferDescription(10000, BufferUsage.VertexBuffer | BufferUsage.Dynamic));
			vertexBuffer.Name = "ImGui.NET Vertex Buffer";
			indexBuffer = gd.ResourceFactory.CreateBuffer(new BufferDescription(2000, BufferUsage.IndexBuffer | BufferUsage.Dynamic));
			indexBuffer.Name = "ImGui.NET Index Buffer";

			RecreateFontDeviceTexture(gd);

			projMatrixBuffer = gd.ResourceFactory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer | BufferUsage.Dynamic));
			projMatrixBuffer.Name = "ImGui.NET Projection Buffer";

			Span<byte> vertexShaderBytes = LoadEmbeddedShaderCode(gd.ResourceFactory, "imgui-vertex");
			Span<byte> fragmentShaderBytes = LoadEmbeddedShaderCode(gd.ResourceFactory, "imgui-frag");
			vertexShader = gd.ResourceFactory.CreateShader(new ShaderDescription(ShaderStages.Vertex, vertexShaderBytes.ToArray(), "main"));
			fragmentShader = gd.ResourceFactory.CreateShader(new ShaderDescription(ShaderStages.Fragment, fragmentShaderBytes.ToArray(), "main"));

			Span<VertexLayoutDescription> vertexLayouts = new VertexLayoutDescription[]
			{
				new VertexLayoutDescription(
					new VertexElementDescription("in_position", VertexElementSemantic.Position, VertexElementFormat.Float2),
					new VertexElementDescription("in_texCoord", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
					new VertexElementDescription("in_color", VertexElementSemantic.Color, VertexElementFormat.Byte4_Norm))
			};

			layout = gd.ResourceFactory.CreateResourceLayout(new ResourceLayoutDescription(
				new ResourceLayoutElementDescription("ProjectionMatrixBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex),
				new ResourceLayoutElementDescription("MainSampler", ResourceKind.Sampler, ShaderStages.Fragment)));
			textureLayout = gd.ResourceFactory.CreateResourceLayout(new ResourceLayoutDescription(
				new ResourceLayoutElementDescription("MainTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment)));

			// TODO: Check pipeline description
			GraphicsPipelineDescription pd = new GraphicsPipelineDescription(
				BlendStateDescription.SingleAlphaBlend,
				new DepthStencilStateDescription(false, false, ComparisonKind.Always),
				new RasterizerStateDescription(FaceCullMode.None, PolygonFillMode.Solid, FrontFace.Clockwise, true, true),
				PrimitiveTopology.TriangleList,
				new ShaderSetDescription(vertexLayouts.ToArray(), new Shader[] { vertexShader, fragmentShader }),
				new ResourceLayout[] { layout, textureLayout },
				outputDescription);
			pipeline = gd.ResourceFactory.CreateGraphicsPipeline(ref pd);

			mainResourceSet = gd.ResourceFactory.CreateResourceSet(new ResourceSetDescription(layout,
				projMatrixBuffer,
				gd.PointSampler));

			fontTextureResourceSet = gd.ResourceFactory.CreateResourceSet(new ResourceSetDescription(textureLayout, fontTextureView));
		}

		/// <summary>
		/// Method responsible for loading custom fonts.
		/// The order in which fonts are provided matters!
		/// </summary>
		/// <param name="gd"> Main graphics device </param>
		/// <param name="fontInfos">Information on the specifics of each font to be loaded</param>
		public void LoadFonts(GraphicsDevice gd, bool replaceExistingFonts, ImGuiFontInfo[] fontInfos)
		{
			unsafe
			{
				ImFontConfigPtr iconConfig = ImGuiNative.ImFontConfig_ImFontConfig();
				iconConfig.OversampleH = 3;
				iconConfig.OversampleV = 3;
				iconConfig.RasterizerMultiply = 1f;
				iconConfig.GlyphExtraSpacing = Vector2.Zero;
				iconConfig.PixelSnapH = true;
				var io = ImGui.GetIO();
				try
				{
					if (replaceExistingFonts)
						io.Fonts.Clear();
					foreach (var fontInfo in fontInfos)
					{
						iconConfig.MergeMode = fontInfo.MergeMode;
						if (fontInfo.IsIconFont)
						{
							GCHandle rangeHandle = GCHandle.Alloc(fontInfo.Ranges, GCHandleType.Pinned);
							try
							{
								io.Fonts.AddFontFromFileTTF(fontInfo.Path, fontInfo.Size, iconConfig, rangeHandle.AddrOfPinnedObject());
							}
							finally
							{
								if (rangeHandle.IsAllocated)
									rangeHandle.Free();
							}
						}
						else
						{
							io.Fonts.AddFontFromFileTTF(fontInfo.Path, fontInfo.Size);
						}
					}
				}
				catch
				{
					io.Fonts.AddFontDefault();
				}
				finally
				{
					ImGuiNative.ImFontConfig_destroy(iconConfig);
				}
				RecreateFontDeviceTexture(gd);
			}
			fontTextureResourceSet = gd.ResourceFactory.CreateResourceSet(new ResourceSetDescription(textureLayout, fontTextureView));
		}

		private IntPtr GetNextImGuiBindingID()
		{
			return (IntPtr)(++lastAssignedID);
		}

		/// <summary>
		/// Gets or creates a handle for a texture to be drawn with ImGui.
		/// Pass the returned handle to Image() or ImageButton().
		/// </summary>
		public IntPtr GetOrCreateImGuiBinding(ResourceFactory factory, TextureView textureView)
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			if (!setsByView.TryGetValue(textureView, out ResourceSetInfo rsi))
			{
				ResourceSet resourceSet = factory.CreateResourceSet(new ResourceSetDescription(textureLayout, textureView));
				rsi = new ResourceSetInfo(GetNextImGuiBindingID(), resourceSet);

				setsByView.Add(textureView, rsi);
				viewsById.Add(rsi.ImGuiBinding, rsi);
				ownedResources.Add(resourceSet);
			}

			return rsi.ImGuiBinding;
		}

		/// <summary>
		/// Gets or creates a handle for a texture to be drawn with ImGui.
		/// Pass the returned handle to Image() or ImageButton().
		/// </summary>
		public IntPtr GetOrCreateImGuiBinding(ResourceFactory factory, Texture texture)
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			if (!autoViewsByTexture.TryGetValue(texture, out TextureView textureView))
			{
				textureView = factory.CreateTextureView(texture);
				autoViewsByTexture.Add(texture, textureView);
				ownedResources.Add(textureView);
			}

			return GetOrCreateImGuiBinding(factory, textureView);
		}

		/// <summary>
		/// Retrieves the shader texture binding for the given helper handle.
		/// </summary>
		public ResourceSet GetImageResourceSet(IntPtr imGuiBinding)
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			if (!viewsById.TryGetValue(imGuiBinding, out ResourceSetInfo tvi))
			{
				throw new InvalidOperationException("No registered ImGui binding with id " + imGuiBinding.ToString());
			}

			return tvi.ResourceSet;
		}

		public void ClearCachedImageResources()
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			foreach (IDisposable resource in ownedResources)
			{
				resource.Dispose();
			}

			ownedResources.Clear();
			setsByView.Clear();
			viewsById.Clear();
			autoViewsByTexture.Clear();
			lastAssignedID = 100;
		}

		private Span<byte> LoadEmbeddedShaderCode(ResourceFactory factory, string name)
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			switch (factory.BackendType)
			{
				case GraphicsBackend.Direct3D11:
					{
						string resourceName = name + ".hlsl.bytes";
						return GetEmbeddedResourceBytes(resourceName);
					}
				case GraphicsBackend.OpenGL:
					{
						string resourceName = name + ".glsl";
						return GetEmbeddedResourceBytes(resourceName);
					}
				case GraphicsBackend.Vulkan:
					{
						string resourceName = name + ".spv";
						return GetEmbeddedResourceBytes(resourceName);
					}
				case GraphicsBackend.Metal:
					{
						string resourceName = name + ".metallib";
						return GetEmbeddedResourceBytes(resourceName);
					}
				default:
					throw new BootEngineException($"{factory.BackendType} backend embedded resources not implemented");
			}
		}

		private Span<byte> GetEmbeddedResourceBytes(string resourceName)
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			Assembly assembly = typeof(ImGuiController).Assembly;
			using (Stream s = assembly.GetManifestResourceStream(resourceName))
			{
				Span<byte> ret = new byte[s.Length];
				s.Read(ret);
				return ret;
			}
		}

		/// <summary>
		/// Recreates the device texture used to render text.
		/// </summary>
		public void RecreateFontDeviceTexture(GraphicsDevice gd)
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			ImGuiIOPtr io = ImGui.GetIO();
			// Build
			io.Fonts.GetTexDataAsRGBA32(out IntPtr pixels, out int width, out int height, out int bytesPerPixel);
			// Store our identifier
			io.Fonts.SetTexID(fontAtlasID);

			fontTexture = gd.ResourceFactory.CreateTexture(TextureDescription.Texture2D(
				(uint)width,
				(uint)height,
				1,
				1,
				PixelFormat.R8_G8_B8_A8_UNorm,
				Veldrid.TextureUsage.Sampled));
			fontTexture.Name = "ImGui.NET Font Texture";
			gd.UpdateTexture(
				fontTexture,
				pixels,
				(uint)(bytesPerPixel * width * height),
				0,
				0,
				0,
				(uint)width,
				(uint)height,
				1,
				0,
				0);
			fontTextureView = gd.ResourceFactory.CreateTextureView(fontTexture);

			io.Fonts.ClearTexData();
		}

		/// <summary>
		/// Renders the ImGui draw list data.
		/// This method requires a <see cref="GraphicsDevice"/> because it may create new DeviceBuffers if the size of vertex
		/// or index data has increased beyond the capacity of the existing buffers.
		/// A <see cref="CommandList"/> is needed to submit drawing and resource update commands.
		/// </summary>
		public void Render(GraphicsDevice gd, CommandList cl)
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			if (frameBegun)
			{
				frameBegun = false;
				ImGui.Render();
				if (!mainWindow.Minimized)
				{
					RenderImDrawData(ImGui.GetDrawData(), gd, cl);
				}

				// Update and Render additional Platform Windows
				if ((ImGui.GetIO().ConfigFlags & ImGuiConfigFlags.ViewportsEnable) != 0)
				{
					ImGui.UpdatePlatformWindows();
					ImGuiPlatformIOPtr platformIO = ImGui.GetPlatformIO();
					for (int i = 1; i < platformIO.Viewports.Size; i++)
					{
						ImGuiViewportPtr vp = platformIO.Viewports[i];
						if ((vp.Flags & ImGuiViewportFlags.Minimized) == 0)
						{
							WindowBase window = (WindowBase)GCHandle.FromIntPtr(vp.PlatformUserData).Target;
							cl.SetFramebuffer(window.Swapchain.Framebuffer);
							RenderImDrawData(vp.DrawData, gd, cl);
						}
					}
				}
			}
		}

		/// <summary>
		/// Swaps Buffers for additional windows created with imgui
		/// </summary>
		public void SwapBuffers(GraphicsDevice gd)
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			if ((ImGui.GetIO().ConfigFlags & ImGuiConfigFlags.ViewportsEnable) != 0)
			{
				ImGuiPlatformIOPtr platformIO = ImGui.GetPlatformIO();
				for (int i = 1; i < platformIO.Viewports.Size; i++)
				{
					ImGuiViewportPtr vp = platformIO.Viewports[i];
					WindowBase window = (WindowBase)GCHandle.FromIntPtr(vp.PlatformUserData).Target;
					gd.SwapBuffers(window.Swapchain);
				}
			}
		}

		/// <summary>
		/// Updates ImGui input and IO configuration state.
		/// </summary>
		public void Update(float deltaSeconds)
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			SetPerFrameImGuiData(deltaSeconds);
			UpdateImGuiInput();
			UpdateMonitors();
		}

		public void BeginFrame()
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			frameBegun = true;
			ImGui.NewFrame();
			ImGuizmo.BeginFrame();
		}

		/// <summary>
		/// Sets per-frame data based on the associated window.
		/// This is called by Update(float).
		/// </summary>
		private void SetPerFrameImGuiData(float deltaSeconds)
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			ImGuiIOPtr io = ImGui.GetIO();
			io.DisplaySize = new Vector2(
				windowWidth / scaleFactor.X,
				windowHeight / scaleFactor.Y);
			io.DisplayFramebufferScale = scaleFactor;
			io.DeltaTime = deltaSeconds; // DeltaTime is in seconds.

			ImGuiPlatformIOPtr plIo = ImGui.GetPlatformIO();
			Sdl2Window mainSdlWindow = mainWindow.SdlWindow;
			plIo.MainViewport.Pos = new Vector2(mainSdlWindow.X, mainSdlWindow.Y);
			plIo.MainViewport.Size = new Vector2(mainSdlWindow.Width, mainSdlWindow.Height);
		}

		private void UpdateImGuiInput()
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			ImGuiIOPtr io = ImGui.GetIO();

			if ((io.ConfigFlags & ImGuiConfigFlags.ViewportsEnable) != 0)
			{
				//Pump events for all windows, but only update inputs regarding the focused one
				ImVector<ImGuiViewportPtr> viewports = ImGui.GetPlatformIO().Viewports;
				for (int i = 0; i < viewports.Size; i++)
				{
					WindowBase window = (WindowBase)GCHandle.FromIntPtr(viewports[i].PlatformUserData).Target;
					bool atualizarSnapshot = window.SdlWindow.Focused;
					window.OnUpdate(atualizarSnapshot);
				}
			}
			else
			{
				mainWindow.OnUpdate();
			}

			InputSnapshot snapshot = InputManager.Snapshot;

			// Determine if any of the mouse buttons were pressed during this snapshot period, even if they are no longer held.
			bool leftPressed = false;
			bool middlePressed = false;
			bool rightPressed = false;
			for (int me = 0; me < snapshot.MouseEvents.Count; me++)
			{
				if (snapshot.MouseEvents[me].Down)
				{
					switch (snapshot.MouseEvents[me].MouseButton)
					{
						case MouseButton.Left:
							leftPressed = true;
							break;
						case MouseButton.Middle:
							middlePressed = true;
							break;
						case MouseButton.Right:
							rightPressed = true;
							break;
					}
				}
			}

			io.MouseDown[0] = leftPressed || snapshot.IsMouseDown(MouseButton.Left);
			io.MouseDown[1] = rightPressed || snapshot.IsMouseDown(MouseButton.Right);
			io.MouseDown[2] = middlePressed || snapshot.IsMouseDown(MouseButton.Middle);

			if ((io.ConfigFlags & ImGuiConfigFlags.ViewportsEnable) != 0)
			{
				int x, y;
				unsafe
				{
					uint buttons = Sdl_GetGlobalMouseState(&x, &y);
					io.MouseDown[0] = (buttons & 0b00001) != 0;
					io.MouseDown[1] = (buttons & 0b00100) != 0;
					io.MouseDown[2] = (buttons & 0b00010) != 0;
					io.MouseDown[3] = (buttons & 0b01000) != 0;
					io.MouseDown[4] = (buttons & 0b10000) != 0;
				}
				io.MousePos = new Vector2(x, y);
			}
			else
			{
				io.MousePos = snapshot.MousePosition;
			}

			io.MouseWheel = snapshot.WheelDelta;

			#region Keyboard
			#region TypedKeys
			IReadOnlyList<char> keyCharPresses = snapshot.KeyCharPresses;
			for (int i = 0; i < keyCharPresses.Count; i++)
			{
				char c = keyCharPresses[i];
				io.AddInputCharacter(c);
			}
			#endregion

			IReadOnlyList<KeyEvent> keyEvents = snapshot.KeyEvents;
			for (int i = 0; i < keyEvents.Count; i++)
			{
				io.KeysDown[(int)keyEvents[i].Key] = keyEvents[i].Down;
			}

			io.KeyCtrl = io.KeysDown[(int)KeyCodes.ControlLeft] || io.KeysDown[(int)KeyCodes.ControlRight];
			io.KeyAlt = io.KeysDown[(int)KeyCodes.AltLeft] || io.KeysDown[(int)KeyCodes.AltRight];
			io.KeyShift = io.KeysDown[(int)KeyCodes.ShiftLeft] || io.KeysDown[(int)KeyCodes.ShiftRight];
			io.KeySuper = io.KeysDown[(int)KeyCodes.SuperLeft] || io.KeysDown[(int)KeyCodes.SuperRight];
			#endregion
		}

		private static void SetImGuiKeyMappings()
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(typeof(ImGuiController));
#endif
			ImGuiIOPtr io = ImGui.GetIO();
			io.KeyMap[(int)ImGuiKey.Tab] = (int)KeyCodes.Tab;
			io.KeyMap[(int)ImGuiKey.LeftArrow] = (int)KeyCodes.Left;
			io.KeyMap[(int)ImGuiKey.RightArrow] = (int)KeyCodes.Right;
			io.KeyMap[(int)ImGuiKey.UpArrow] = (int)KeyCodes.Up;
			io.KeyMap[(int)ImGuiKey.DownArrow] = (int)KeyCodes.Down;
			io.KeyMap[(int)ImGuiKey.PageUp] = (int)KeyCodes.PageUp;
			io.KeyMap[(int)ImGuiKey.PageDown] = (int)KeyCodes.PageDown;
			io.KeyMap[(int)ImGuiKey.Home] = (int)KeyCodes.Home;
			io.KeyMap[(int)ImGuiKey.End] = (int)KeyCodes.End;
			io.KeyMap[(int)ImGuiKey.Insert] = (int)KeyCodes.Insert;
			io.KeyMap[(int)ImGuiKey.Delete] = (int)KeyCodes.Delete;
			io.KeyMap[(int)ImGuiKey.Backspace] = (int)KeyCodes.BackSpace;
			io.KeyMap[(int)ImGuiKey.Space] = (int)KeyCodes.Space;
			io.KeyMap[(int)ImGuiKey.Enter] = (int)KeyCodes.Enter;
			io.KeyMap[(int)ImGuiKey.Escape] = (int)KeyCodes.Escape;
			io.KeyMap[(int)ImGuiKey.KeyPadEnter] = (int)KeyCodes.KeypadEnter;
			io.KeyMap[(int)ImGuiKey.A] = (int)KeyCodes.A;
			io.KeyMap[(int)ImGuiKey.C] = (int)KeyCodes.C;
			io.KeyMap[(int)ImGuiKey.V] = (int)KeyCodes.V;
			io.KeyMap[(int)ImGuiKey.X] = (int)KeyCodes.X;
			io.KeyMap[(int)ImGuiKey.Y] = (int)KeyCodes.Y;
			io.KeyMap[(int)ImGuiKey.Z] = (int)KeyCodes.Z;
		}

		private void RenderImDrawData(ImDrawDataPtr draw_data, GraphicsDevice gd, CommandList cl)
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			if (draw_data.CmdListsCount == 0)
			{
				return;
			}

			uint vertexOffsetInVertices = 0;
			uint indexOffsetInElements = 0;

			uint totalVBSize = (uint)(draw_data.TotalVtxCount * Unsafe.SizeOf<ImDrawVert>());
			if (totalVBSize > vertexBuffer.SizeInBytes)
			{
				gd.DisposeWhenIdle(vertexBuffer);
				vertexBuffer = gd.ResourceFactory.CreateBuffer(new BufferDescription((uint)(totalVBSize * 1.5f), BufferUsage.VertexBuffer | BufferUsage.Dynamic));
			}

			uint totalIBSize = (uint)(draw_data.TotalIdxCount * sizeof(ushort));
			if (totalIBSize > indexBuffer.SizeInBytes)
			{
				gd.DisposeWhenIdle(indexBuffer);
				indexBuffer = gd.ResourceFactory.CreateBuffer(new BufferDescription((uint)(totalIBSize * 1.5f), BufferUsage.IndexBuffer | BufferUsage.Dynamic));
			}

			for (int i = 0; i < draw_data.CmdListsCount; i++)
			{
				ImDrawListPtr cmd_list = draw_data.CmdListsRange[i];

				cl.UpdateBuffer(
					vertexBuffer,
					vertexOffsetInVertices * (uint)Unsafe.SizeOf<ImDrawVert>(),
					cmd_list.VtxBuffer.Data,
					(uint)(cmd_list.VtxBuffer.Size * Unsafe.SizeOf<ImDrawVert>()));

				cl.UpdateBuffer(
					indexBuffer,
					indexOffsetInElements * sizeof(ushort),
					cmd_list.IdxBuffer.Data,
					(uint)(cmd_list.IdxBuffer.Size * sizeof(ushort)));

				vertexOffsetInVertices += (uint)cmd_list.VtxBuffer.Size;
				indexOffsetInElements += (uint)cmd_list.IdxBuffer.Size;
			}

			//Setup orthographic projection matrix regarding backend (view matrix is identity)
			Matrix4x4 proj;
			if (gd.BackendType == GraphicsBackend.Vulkan)
			{
				proj = new ImGuiCamera(
				draw_data.DisplayPos.X,
				draw_data.DisplayPos.X + draw_data.DisplaySize.X,
				draw_data.DisplayPos.Y,
				draw_data.DisplayPos.Y + draw_data.DisplaySize.Y).ProjectionMatrix;
			}
			else
			{
				proj = new ImGuiCamera(
				draw_data.DisplayPos.X,
				draw_data.DisplayPos.X + draw_data.DisplaySize.X,
				draw_data.DisplayPos.Y + draw_data.DisplaySize.Y,
				draw_data.DisplayPos.Y).ProjectionMatrix;
			}

			cl.SetPipeline(pipeline);
			cl.SetFullViewports();
			cl.SetVertexBuffer(0, vertexBuffer);
			cl.SetIndexBuffer(indexBuffer, IndexFormat.UInt16);
			cl.UpdateBuffer(projMatrixBuffer, 0, ref proj);
			cl.SetGraphicsResourceSet(0, mainResourceSet);

			draw_data.ScaleClipRects(draw_data.FramebufferScale);

			// Render command lists
			int vtx_offset = 0;
			int idx_offset = 0;
			for (int n = 0; n < draw_data.CmdListsCount; n++)
			{
				ImDrawListPtr cmd_list = draw_data.CmdListsRange[n];
				for (int cmd_i = 0; cmd_i < cmd_list.CmdBuffer.Size; cmd_i++)
				{
					ImDrawCmdPtr pcmd = cmd_list.CmdBuffer[cmd_i];
					if (pcmd.UserCallback != IntPtr.Zero)
					{
						throw new BootEngineException("ImGUI command user callback not implemented.");
					}
					else
					{
						if (pcmd.TextureId != IntPtr.Zero)
						{
							if (pcmd.TextureId == fontAtlasID)
							{
								cl.SetGraphicsResourceSet(1, fontTextureResourceSet);
							}
							else
							{
								cl.SetGraphicsResourceSet(1, GetImageResourceSet(pcmd.TextureId));
							}
						}

						Vector2 clipOff = draw_data.DisplayPos;
						Vector2 clipScale = draw_data.FramebufferScale;

						cl.SetScissorRect(
							0,
							(uint)((pcmd.ClipRect.X - clipOff.X) * clipScale.X),
							(uint)((pcmd.ClipRect.Y - clipOff.Y) * clipScale.Y),
							(uint)((pcmd.ClipRect.Z - clipOff.X) * clipScale.X),
							(uint)((pcmd.ClipRect.W - clipOff.Y) * clipScale.Y));

						cl.DrawIndexed(pcmd.ElemCount, 1, (uint)idx_offset, vtx_offset, 0);
					}

					idx_offset += (int)pcmd.ElemCount;
				}
				vtx_offset += cmd_list.VtxBuffer.Size;
			}
		}

		/// <summary>
		/// Frees all graphics resources used by the renderer.
		/// </summary>
		public void Dispose()
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			unsafe
			{
				var plIo = ImGui.GetPlatformIO();
				Marshal.FreeHGlobal(plIo.NativePtr->Monitors.Data);
				plIo.NativePtr->Monitors = new ImVector();
			}

			ClearCachedImageResources();

			ImGui.DestroyContext();
			ImPlot.DestroyContext();
			ImNodes.Shutdown();

			pipeline.Dispose();
			vertexBuffer.Dispose();
			indexBuffer.Dispose();
			projMatrixBuffer.Dispose();
			fontTextureResourceSet.Dispose();
			fontTextureView.Dispose();
			fontTexture.Dispose();
			vertexShader.Dispose();
			fragmentShader.Dispose();
			mainResourceSet.Dispose();
			layout.Dispose();
			textureLayout.Dispose();
		}

		private void SetupImGuiIo(Sdl2Window sdlWindow)
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			ImGuiIOPtr io = ImGui.GetIO();
			unsafe
			{
				io.NativePtr->BackendPlatformName = (byte*)new FixedAsciiString("Veldrid.SDL2 Backend").DataPtr;
			}

			io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard | ImGuiConfigFlags.DockingEnable;

			io.ConfigWindowsMoveFromTitleBarOnly = true;
			io.ConfigViewportsNoTaskBarIcon = true;

			io.BackendFlags |= ImGuiBackendFlags.HasMouseCursors | ImGuiBackendFlags.HasSetMousePos | ImGuiBackendFlags.RendererHasVtxOffset;

			// TODO: Enable viewports for OpenGL
			if (graphicsDevice.BackendType != GraphicsBackend.OpenGL)
			{
				io.ConfigFlags |= ImGuiConfigFlags.ViewportsEnable;
				io.BackendFlags |= ImGuiBackendFlags.RendererHasViewports;
				io.BackendFlags |= ImGuiBackendFlags.PlatformHasViewports;// | ImGuiBackendFlags.HasMouseHoveredViewport;
			}

			if ((io.ConfigFlags & ImGuiConfigFlags.ViewportsEnable) != 0)
			{
				SetupPlatformIo(sdlWindow);
			}
		}

		private unsafe void SetupPlatformIo(Sdl2Window sdlWindow)
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			ImGuiStylePtr style = ImGui.GetStyle();
			style.WindowRounding = 0.0f;
			style.Colors[(int)ImGuiCol.WindowBg].W = 1.0f;

			createWindow = PlatformCreateWindow;
			destroyWindow = PlatformDestroyWindow;
			showWindow = PlatformShowWindow;
			getWindowPosition = PlatformGetWindowPosition;
			setWindowPosition = PlatformSetWindowPosition;
			getWindowSize = PlatformGetWindowSize;
			setWindowSize = PlatformSetWindowSize;
			setWindowTitle = PlatformSetWindowTitle;
			getWindowFocus = PlatformGetWindowFocus;
			setWindowFocus = PlatformSetWindowFocus;
			getWindowMinimized = PlatformGetWindowMinimized;
			setWindowAlpha = PlatformSetWindowAlpha;

			createVulkanSurface = PlatformCreateVkSurface;

			ImGuiPlatformIOPtr plIo = ImGui.GetPlatformIO();
			plIo.NativePtr->Platform_CreateWindow = Marshal.GetFunctionPointerForDelegate(createWindow);
			plIo.NativePtr->Platform_DestroyWindow = Marshal.GetFunctionPointerForDelegate(destroyWindow);
			plIo.NativePtr->Platform_ShowWindow = Marshal.GetFunctionPointerForDelegate(showWindow);
			plIo.NativePtr->Platform_GetWindowPos = Marshal.GetFunctionPointerForDelegate(getWindowPosition);
			plIo.NativePtr->Platform_SetWindowPos = Marshal.GetFunctionPointerForDelegate(setWindowPosition);
			plIo.NativePtr->Platform_GetWindowSize = Marshal.GetFunctionPointerForDelegate(getWindowSize);
			plIo.NativePtr->Platform_SetWindowSize = Marshal.GetFunctionPointerForDelegate(setWindowSize);
			plIo.NativePtr->Platform_SetWindowTitle = Marshal.GetFunctionPointerForDelegate(setWindowTitle);
			plIo.NativePtr->Platform_GetWindowMinimized = Marshal.GetFunctionPointerForDelegate(getWindowMinimized);
			plIo.NativePtr->Platform_GetWindowFocus = Marshal.GetFunctionPointerForDelegate(getWindowFocus);
			plIo.NativePtr->Platform_SetWindowFocus = Marshal.GetFunctionPointerForDelegate(setWindowFocus);
			plIo.NativePtr->Platform_SetWindowAlpha = Marshal.GetFunctionPointerForDelegate(setWindowAlpha);
			plIo.NativePtr->Platform_CreateVkSurface = Marshal.GetFunctionPointerForDelegate(createVulkanSurface);

			UpdateMonitors();

			ImGuiViewportPtr mainViewport = plIo.MainViewport;
			mainViewport.PlatformHandle = sdlWindow.Handle;
			mainViewport.PlatformUserData = (IntPtr)mainWindow.GcHandle;
		}

		private unsafe static void UpdateMonitors()
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(typeof(ImGuiController));
#endif
			ImGuiPlatformIOPtr plIo = ImGui.GetPlatformIO();
			Marshal.FreeHGlobal(plIo.NativePtr->Monitors.Data);
			int numMonitors = Sdl_GetNumVideoDisplays();
			IntPtr data = Marshal.AllocHGlobal(Unsafe.SizeOf<ImGuiPlatformMonitor>() * numMonitors);
			plIo.NativePtr->Monitors = new ImVector(numMonitors, numMonitors, data);
			for (int i = 0; i < numMonitors; i++)
			{
				Rectangle r;
				Sdl_GetDisplayUsableBounds(i, &r);
				ImGuiPlatformMonitorPtr monitor = plIo.Monitors[i];
				monitor.DpiScale = 1f;
				monitor.MainPos = new Vector2(r.X, r.Y);
				monitor.MainSize = new Vector2(r.Width, r.Height);
				monitor.WorkPos = new Vector2(r.X, r.Y);
				monitor.WorkSize = new Vector2(r.Width, r.Height);
			}
		}

		private unsafe bool PlatformCreateVkSurface(ImGuiViewportPtr viewport)
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			//TODO: Properly avoid memory leak for unreleased Vk surface
			WindowBase window = (WindowBase)GCHandle.FromIntPtr(viewport.PlatformUserData).Target;
			SDL_SysWMinfo sysWmInfo;
			Sdl2Native.SDL_GetVersion(&sysWmInfo.version);
			Sdl2Native.SDL_GetWMWindowInfo(window.SdlWindow.SdlWindowHandle, &sysWmInfo);
			return GetSurfaceSource(sysWmInfo).CreateSurface(new Vulkan.VkInstance(window.SdlWindow.SdlWindowHandle)) != Vulkan.VkSurfaceKHR.Null;
		}

		private unsafe static VkSurfaceSource GetSurfaceSource(SDL_SysWMinfo sysWmInfo)
		{
			switch (sysWmInfo.subsystem)
			{
				case SysWMType.Windows:
					Win32WindowInfo w32Info = Unsafe.Read<Win32WindowInfo>(&sysWmInfo.info);
					return VkSurfaceSource.CreateWin32(w32Info.hinstance, w32Info.Sdl2Window);
				case SysWMType.X11:
					X11WindowInfo x11Info = Unsafe.Read<X11WindowInfo>(&sysWmInfo.info);
					return VkSurfaceSource.CreateXlib(
						(Vulkan.Xlib.Display*)x11Info.display,
						new Vulkan.Xlib.Window() { Value = x11Info.Sdl2Window });
				default:
					throw new PlatformNotSupportedException("Cannot create a Vulkan surface for " + sysWmInfo.subsystem + ".");
			}
		}

		private byte PlatformGetWindowMinimized(ImGuiViewportPtr viewport)
		{
			WindowBase window = (WindowBase)GCHandle.FromIntPtr(viewport.PlatformUserData).Target;
			return (Sdl2Native.SDL_GetWindowFlags(window.SdlWindow.SdlWindowHandle) & SDL_WindowFlags.Minimized) != 0 ? (byte)1 : (byte)0;
		}

		private bool PlatformGetWindowFocus(ImGuiViewportPtr viewport)
		{
			WindowBase window = (WindowBase)GCHandle.FromIntPtr(viewport.PlatformUserData).Target;
			return (Sdl2Native.SDL_GetWindowFlags(window.SdlWindow.SdlWindowHandle) & SDL_WindowFlags.InputFocus) != 0;
		}

		private static void PlatformSetWindowFocus(ImGuiViewportPtr viewport)
		{
			WindowBase window = (WindowBase)GCHandle.FromIntPtr(viewport.PlatformUserData).Target;
			Sdl_RaiseWindow(window.SdlWindow.SdlWindowHandle);
		}

		private void PlatformSetWindowAlpha(ImGuiViewportPtr viewport, float alpha)
		{
			WindowBase window = (WindowBase)GCHandle.FromIntPtr(viewport.PlatformUserData).Target;
			window.SdlWindow.Opacity = alpha;
		}

		private void PlatformSetWindowTitle(ImGuiViewportPtr viewport, string title)
		{
			WindowBase window = (WindowBase)GCHandle.FromIntPtr(viewport.PlatformUserData).Target;
			window.SdlWindow.Title = title;
		}

		private void PlatformSetWindowPosition(ImGuiViewportPtr viewport, Vector2 pos)
		{
			WindowBase window = (WindowBase)GCHandle.FromIntPtr(viewport.PlatformUserData).Target;
			window.SdlWindow.X = (int)pos.X;
			window.SdlWindow.Y = (int)pos.Y;
		}

		private void PlatformSetWindowSize(ImGuiViewportPtr viewport, Vector2 size)
		{
			WindowBase window = (WindowBase)GCHandle.FromIntPtr(viewport.PlatformUserData).Target;
			Sdl2Native.SDL_SetWindowSize(window.SdlWindow.SdlWindowHandle, (int)size.X, (int)size.Y);
		}

		private void PlatformGetWindowSize(Vector2 size, ImGuiViewportPtr viewport)
		{
			//Seems to work
			WindowBase window = (WindowBase)GCHandle.FromIntPtr(viewport.PlatformUserData).Target;
			Rectangle bounds = window.SdlWindow.Bounds;
			size.X = bounds.Width;
			size.Y = bounds.Height;
		}

		private void PlatformGetWindowPosition(Vector2 pos, ImGuiViewportPtr viewport)
		{
			WindowBase window = (WindowBase)GCHandle.FromIntPtr(viewport.PlatformUserData).Target;
			Rectangle bounds = window.SdlWindow.Bounds;
			pos.X = bounds.X;
			pos.Y = bounds.Y;
		}

		private unsafe void PlatformCreateWindow(ImGuiViewportPtr viewport)
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			SDL_WindowFlags sdlFlags = (Sdl2Native.SDL_GetWindowFlags(mainWindow.SdlWindow.SdlWindowHandle) & SDL_WindowFlags.AllowHighDpi)
				| SDL_WindowFlags.Hidden;
			sdlFlags |= ((viewport.Flags & ImGuiViewportFlags.NoDecoration) != 0) ? SDL_WindowFlags.Borderless : SDL_WindowFlags.Resizable;

			if (graphicsDevice.BackendType == GraphicsBackend.OpenGL || graphicsDevice.BackendType == GraphicsBackend.OpenGLES)
				sdlFlags |= SDL_WindowFlags.OpenGL;
			if ((viewport.Flags & ImGuiViewportFlags.TopMost) != 0)
				sdlFlags |= SDL_WindowFlags.AlwaysOnTop;
			//Seems to work
			if ((viewport.Flags & ImGuiViewportFlags.NoTaskBarIcon) != 0)
				sdlFlags |= SDL_WindowFlags.SkipTaskbar;

			Sdl2Window sdlWindow = new Sdl2Window("Viewport", (int)viewport.Pos.X, (int)viewport.Pos.Y,
				(int)viewport.Size.X, (int)viewport.Size.Y, sdlFlags, false);

			sdlWindow.Resized += () => viewport.PlatformRequestResize = true;
			sdlWindow.Moved += (_) => viewport.PlatformRequestMove = true;
			sdlWindow.Closed += () => viewport.PlatformRequestClose = true;

			WindowBase newWindow = WindowBase.CreateSubWindow(graphicsDevice, sdlWindow, mainWindow.GetType());

			viewport.PlatformUserData = (IntPtr)newWindow.GcHandle;
			viewport.PlatformHandle = newWindow.SdlWindow.Handle;
		}

		private void PlatformDestroyWindow(ImGuiViewportPtr viewport)
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			if (viewport.PlatformUserData != IntPtr.Zero)
			{
				WindowBase window = (WindowBase)GCHandle.FromIntPtr(viewport.PlatformUserData).Target;
				window.Dispose();
				viewport.PlatformUserData = IntPtr.Zero;
			}
		}

		private unsafe void PlatformShowWindow(ImGuiViewportPtr viewport)
		{
			WindowBase window = (WindowBase)GCHandle.FromIntPtr(viewport.PlatformUserData).Target;
			Sdl2Native.SDL_ShowWindow(window.SdlWindow.SdlWindowHandle);
		}

		#region Structs
		private readonly struct ResourceSetInfo
		{
			public readonly IntPtr ImGuiBinding;
			public readonly ResourceSet ResourceSet;

			public ResourceSetInfo(IntPtr imGuiBinding, ResourceSet resourceSet)
			{
				ImGuiBinding = imGuiBinding;
				ResourceSet = resourceSet;
			}
		}
		#endregion
	}
}
