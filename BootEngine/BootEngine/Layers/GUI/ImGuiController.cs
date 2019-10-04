using BootEngine.Input;
using BootEngine.Window;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Utils;
using Utils.Exceptions;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.Vk;

namespace BootEngine.Layers.GUI
{
    public sealed class ImGuiController : IDisposable
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

        private readonly IntPtr fontAtlasID = (IntPtr)1;

        private int windowWidth;
        private int windowHeight;
        private Vector2 scaleFactor = Vector2.One;

		#region Delegates
		private delegate void ViewportDelegate(ImGuiViewportPtr viewport);
		private delegate Vector2 ViewportVec2Delegate(ImGuiViewportPtr viewport);
		private delegate bool ViewportBoolDelegate(ImGuiViewportPtr viewport);
		private delegate void ArgVec2Delegate(ImGuiViewportPtr viewport, Vector2 arg0);
		private delegate void ArgStringDelegate(ImGuiViewportPtr viewport, string arg0);
		private delegate void ArgFloatDelegate(ImGuiViewportPtr viewport, float arg0);
		#endregion

		// Image trackers
		private readonly Dictionary<TextureView, ResourceSetInfo> setsByView = new Dictionary<TextureView, ResourceSetInfo>();
        private readonly Dictionary<Texture, TextureView> autoViewsByTexture = new Dictionary<Texture, TextureView>();
        private readonly Dictionary<IntPtr, ResourceSetInfo> viewsById = new Dictionary<IntPtr, ResourceSetInfo>();
        private readonly List<IDisposable> ownedResources = new List<IDisposable>();
        private int lastAssignedID = 100;

		//Windows
		private readonly long WS_EX_APPWINDOW = 0x00040000L;
		private readonly long WS_EX_TOOLWINDOW = 0x00000080L;
		private const int GWL_EXSTYLE = -20;
		private const int SW_SHOWNA = 8;
		private const int SM_CMONITORS = 80;

		[DllImport("user32.dll")]
		static extern int GetSystemMetrics(int smIndex);
		[DllImport("user32.dll", EntryPoint = "GetWindowLong")]
		private static extern IntPtr GetWindowLongPtr32(IntPtr hWnd, int nIndex);
		[DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
		private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);
		[DllImport("user32.dll", EntryPoint = "SetWindowLong")]
		private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);
		[DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
		private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);
		[DllImport("user32.dll")]
		static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

		// This static method is required because Win32 does not support
		// GetWindowLongPtr directly
		public static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
		{
			if (IntPtr.Size == 8)
				return GetWindowLongPtr64(hWnd, nIndex);
			else
				return GetWindowLongPtr32(hWnd, nIndex);
		}
		// This static method is required because legacy OSes do not support
		// SetWindowLongPtr
		public static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
		{
			if (IntPtr.Size == 8)
				return SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
			else
				return new IntPtr(SetWindowLong32(hWnd, nIndex, dwNewLong.ToInt32()));
		}
		#endregion

		/// <summary>
		/// Constructs a new ImGuiController.
		/// </summary>
		public ImGuiController(GraphicsDevice gd, OutputDescription outputDescription, Sdl2Window sdlWindow)
        {
            windowWidth = sdlWindow.Width;
            windowHeight = sdlWindow.Height;

            ImGui.SetCurrentContext(ImGui.CreateContext());
			ImGui.StyleColorsDark();

			CreateDeviceResources(gd, outputDescription);

			SetupImGuiIo(sdlWindow);
            SetImGuiKeyMappings();

            SetPerFrameImGuiData(1f / 60);
        }

        public void WindowResized(int width, int height)
        {
            windowWidth = width;
            windowHeight = height;
        }

        public void CreateDeviceResources(GraphicsDevice gd, OutputDescription outputDescription)
        {
            graphicsDevice = gd;
            vertexBuffer = gd.ResourceFactory.CreateBuffer(new BufferDescription(10000, BufferUsage.VertexBuffer | BufferUsage.Dynamic));
            vertexBuffer.Name = "ImGui.NET Vertex Buffer";
            indexBuffer = gd.ResourceFactory.CreateBuffer(new BufferDescription(2000, BufferUsage.IndexBuffer | BufferUsage.Dynamic));
            indexBuffer.Name = "ImGui.NET Index Buffer";
            RecreateFontDeviceTexture(gd);

            projMatrixBuffer = gd.ResourceFactory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer | BufferUsage.Dynamic));
            projMatrixBuffer.Name = "ImGui.NET Projection Buffer";

            byte[] vertexShaderBytes = LoadEmbeddedShaderCode(gd.ResourceFactory, "imgui-vertex");
            byte[] fragmentShaderBytes = LoadEmbeddedShaderCode(gd.ResourceFactory, "imgui-frag");
            vertexShader = gd.ResourceFactory.CreateShader(new ShaderDescription(ShaderStages.Vertex, vertexShaderBytes, "VS"));
            fragmentShader = gd.ResourceFactory.CreateShader(new ShaderDescription(ShaderStages.Fragment, fragmentShaderBytes, "FS"));

            VertexLayoutDescription[] vertexLayouts = new VertexLayoutDescription[]
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

            GraphicsPipelineDescription pd = new GraphicsPipelineDescription(
                BlendStateDescription.SingleAlphaBlend,
                new DepthStencilStateDescription(false, false, ComparisonKind.Always),
                new RasterizerStateDescription(FaceCullMode.None, PolygonFillMode.Solid, FrontFace.Clockwise, false, true),
                PrimitiveTopology.TriangleList,
                new ShaderSetDescription(vertexLayouts, new[] { vertexShader, fragmentShader }),
                new ResourceLayout[] { layout, textureLayout },
                outputDescription);
            pipeline = gd.ResourceFactory.CreateGraphicsPipeline(ref pd);

            mainResourceSet = gd.ResourceFactory.CreateResourceSet(new ResourceSetDescription(layout,
                projMatrixBuffer,
                gd.PointSampler));

            fontTextureResourceSet = gd.ResourceFactory.CreateResourceSet(new ResourceSetDescription(textureLayout, fontTextureView));
        }

        /// <summary>
        /// Gets or creates a handle for a texture to be drawn with ImGui.
        /// Pass the returned handle to Image() or ImageButton().
        /// </summary>
        public IntPtr GetOrCreateImGuiBinding(ResourceFactory factory, TextureView textureView)
        {
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

        private IntPtr GetNextImGuiBindingID()
        {
            return (IntPtr)(++lastAssignedID);
        }

        /// <summary>
        /// Gets or creates a handle for a texture to be drawn with ImGui.
        /// Pass the returned handle to Image() or ImageButton().
        /// </summary>
        public IntPtr GetOrCreateImGuiBinding(ResourceFactory factory, Texture texture)
        {
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
            if (!viewsById.TryGetValue(imGuiBinding, out ResourceSetInfo tvi))
            {
                throw new InvalidOperationException("No registered ImGui binding with id " + imGuiBinding.ToString());
            }

            return tvi.ResourceSet;
        }

        public void ClearCachedImageResources()
        {
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

        private byte[] LoadEmbeddedShaderCode(ResourceFactory factory, string name)
        {
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

        private byte[] GetEmbeddedResourceBytes(string resourceName)
        {
            Assembly assembly = typeof(ImGuiController).Assembly;
            using (Stream s = assembly.GetManifestResourceStream(resourceName))
            {
                byte[] ret = new byte[s.Length];
                s.Read(ret, 0, (int)s.Length);
                return ret;
            }
        }

        /// <summary>
        /// Recreates the device texture used to render text.
        /// </summary>
        public void RecreateFontDeviceTexture(GraphicsDevice gd)
        {
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
                TextureUsage.Sampled));
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
            if (frameBegun)
            {
                frameBegun = false;
                ImGui.Render();
                RenderImDrawData(ImGui.GetDrawData(), gd, cl);
            }
        }

        /// <summary>
        /// Updates ImGui input and IO configuration state.
        /// </summary>
        public void Update(float deltaSeconds)
        {
            SetPerFrameImGuiData(deltaSeconds);
            UpdateImGuiInput(InputManager.Snapshot);
        }

        public void BeginFrame()
        {
            frameBegun = true;
            ImGui.NewFrame();
        }

        /// <summary>
        /// Sets per-frame data based on the associated window.
        /// This is called by Update(float).
        /// </summary>
        private void SetPerFrameImGuiData(float deltaSeconds)
        {
            ImGuiIOPtr io = ImGui.GetIO();
            io.DisplaySize = new Vector2(
                windowWidth / scaleFactor.X,
                windowHeight / scaleFactor.Y);
            io.DisplayFramebufferScale = scaleFactor;
            io.DeltaTime = deltaSeconds; // DeltaTime is in seconds.
        }

        private void UpdateImGuiInput(InputSnapshot snapshot)
        {
            if (snapshot == null)
                return;

            ImGuiIOPtr io = ImGui.GetIO();

            // Determine if any of the mouse buttons were pressed during this snapshot period, even if they are no longer held.
            bool leftPressed = false;
            bool middlePressed = false;
            bool rightPressed = false;
            foreach (MouseEvent me in snapshot.MouseEvents)
            {
                if (me.Down)
                {
                    switch (me.MouseButton)
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
            io.MousePos = snapshot.MousePosition;
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
                KeyEvent keyEvent = keyEvents[i];
				io.KeysDown[(int)keyEvent.Key] = keyEvent.Down;
            }

            io.KeyCtrl = io.KeysDown[(int)KeyCodes.ControlLeft] || io.KeysDown[(int)KeyCodes.ControlRight];
            io.KeyAlt = io.KeysDown[(int)KeyCodes.AltLeft] || io.KeysDown[(int)KeyCodes.AltRight];
            io.KeyShift = io.KeysDown[(int)KeyCodes.ShiftLeft] || io.KeysDown[(int)KeyCodes.ShiftRight];
            io.KeySuper = io.KeysDown[(int)KeyCodes.SuperLeft] || io.KeysDown[(int)KeyCodes.SuperRight];
            #endregion
        }

        private static void SetImGuiKeyMappings()
        {
			//TODO fix the useless +4 being added, check for Modifiers that are not working
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

            // Setup orthographic projection matrix into our constant buffer
            ImGuiIOPtr io = ImGui.GetIO();
            Matrix4x4 mvp = Matrix4x4.CreateOrthographicOffCenter(
                0f,
                io.DisplaySize.X,
                io.DisplaySize.Y,
                0.0f,
                -1.0f,
                1.0f);

            graphicsDevice.UpdateBuffer(projMatrixBuffer, 0, ref mvp);

            cl.SetVertexBuffer(0, vertexBuffer);
            cl.SetIndexBuffer(indexBuffer, IndexFormat.UInt16);
            cl.SetPipeline(pipeline);
            cl.SetGraphicsResourceSet(0, mainResourceSet);

            draw_data.ScaleClipRects(io.DisplayFramebufferScale);

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

                        cl.SetScissorRect(
                            0,
                            (uint)pcmd.ClipRect.X,
                            (uint)pcmd.ClipRect.Y,
                            (uint)(pcmd.ClipRect.Z - pcmd.ClipRect.X),
                            (uint)(pcmd.ClipRect.W - pcmd.ClipRect.Y));

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
            vertexBuffer.Dispose();
            indexBuffer.Dispose();
            projMatrixBuffer.Dispose();
            fontTexture.Dispose();
            fontTextureView.Dispose();
            vertexShader.Dispose();
            fragmentShader.Dispose();
            layout.Dispose();
            textureLayout.Dispose();
            pipeline.Dispose();
            mainResourceSet.Dispose();
            fontTextureResourceSet.Dispose();

            foreach (IDisposable resource in ownedResources)
            {
                resource.Dispose();
            }
        }

		private void SetupImGuiIo(Sdl2Window sdlWindow)
		{
			ImGuiIOPtr io = ImGui.GetIO();
			io.Fonts.AddFontDefault();
			io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard | ImGuiConfigFlags.DockingEnable;
			io.ConfigFlags |= ImGuiConfigFlags.ViewportsEnable;
			io.BackendFlags |= ImGuiBackendFlags.HasMouseCursors | ImGuiBackendFlags.HasSetMousePos;
			//io.BackendFlags |= ImGuiBackendFlags.RendererHasViewports;
			io.BackendFlags |= ImGuiBackendFlags.PlatformHasViewports | ImGuiBackendFlags.HasMouseHoveredViewport;

			if ((io.ConfigFlags & ImGuiConfigFlags.ViewportsEnable) != 0)
			{
				if (graphicsDevice.BackendType == GraphicsBackend.OpenGL || graphicsDevice.BackendType == GraphicsBackend.OpenGLES)
					SetupPlatformIo(sdlWindow, Sdl2Native.SDL_GL_CreateContext(sdlWindow.SdlWindowHandle));
				else
					SetupPlatformIo(sdlWindow, IntPtr.Zero);
			}
		}

		private unsafe void SetupPlatformIo(Sdl2Window sdlWindow, IntPtr sdlGlContext)
		{
			ImGuiStylePtr style = ImGui.GetStyle();
			style.WindowRounding = 0.0f;
			style.Colors[(int)ImGuiCol.WindowBg].W = 1.0f;

			ImGuiPlatformIOPtr plIo = ImGui.GetPlatformIO();
			plIo.Platform_CreateWindow = Marshal.GetFunctionPointerForDelegate<ViewportDelegate>(PlatformCreateWindow);
			plIo.Platform_DestroyWindow = Marshal.GetFunctionPointerForDelegate<ViewportDelegate>(PlatformDestroyWindow);
			plIo.Platform_ShowWindow = Marshal.GetFunctionPointerForDelegate<ViewportDelegate>(PlatformShowWindow);
			plIo.Platform_GetWindowPos = Marshal.GetFunctionPointerForDelegate<ViewportVec2Delegate>(PlatformGetWindowPosition);
			plIo.Platform_SetWindowPos = Marshal.GetFunctionPointerForDelegate<ArgVec2Delegate>(PlatformSetWindowPosition);
			plIo.Platform_GetWindowSize = Marshal.GetFunctionPointerForDelegate<ViewportVec2Delegate>(PlatformGetWindowSize);
			plIo.Platform_SetWindowSize = Marshal.GetFunctionPointerForDelegate<ArgVec2Delegate>(PlatformSetWindowSize);
			plIo.Platform_SetWindowTitle = Marshal.GetFunctionPointerForDelegate<ArgStringDelegate>(PlatformSetWindowTitle);
			plIo.Platform_SetWindowAlpha = Marshal.GetFunctionPointerForDelegate<ArgFloatDelegate>(PlatformSetWindowAlpha);
			plIo.Platform_GetWindowFocus = Marshal.GetFunctionPointerForDelegate<ViewportBoolDelegate>(PlatformGetWindowFocus);
			plIo.Platform_GetWindowMinimized = Marshal.GetFunctionPointerForDelegate<ViewportBoolDelegate>(PlatformGetWindowMinimized);
			plIo.Platform_RenderWindow = Marshal.GetFunctionPointerForDelegate<ViewportDelegate>(PlatformRenderWindow);
			plIo.Platform_SwapBuffers = Marshal.GetFunctionPointerForDelegate<ViewportDelegate>(PlatformSwapBuffers);
			plIo.Platform_CreateVkSurface = Marshal.GetFunctionPointerForDelegate<ViewportBoolDelegate>(PlatformCreateVkSurface);

			//not yet implemented
			plIo.Platform_SetWindowFocus = Marshal.GetFunctionPointerForDelegate<ViewportDelegate>(PlatformSetWindowFocus);

			UpdateMonitors(plIo);

			ImGuiViewportPtr mainViewport = ImGui.GetMainViewport();
			ViewportDataPtr data = new ViewportDataPtr((IntPtr)mainViewport.NativePtr);
			data.SdlWindowHandle = sdlWindow.SdlWindowHandle;
			data.WindowID = Sdl2Native.SDL_GetWindowID(sdlWindow.SdlWindowHandle);
			data.WindowOwned = false;
			data.GlContext = sdlGlContext;
			mainViewport.PlatformUserData = (IntPtr)data.NativePtr;
			mainViewport.PlatformHandle = sdlWindow.SdlWindowHandle;
			mainViewport.PlatformHandleRaw = sdlWindow.Handle;
		}

		private unsafe void UpdateMonitors(ImGuiPlatformIOPtr plIo)
		{
			//TODO: rework stuff around new sdl lib
			int displayCount = SDL2.SDL.SDL_GetNumVideoDisplays();
			for (int n = 0; n < displayCount; n++)
			{
				ImGuiPlatformMonitorPtr monitor = ImGuiNative.ImGuiPlatformMonitor_ImGuiPlatformMonitor();
				SDL2.SDL.SDL_GetDisplayBounds(n, out SDL2.SDL.SDL_Rect r);
				monitor.MainPos = new Vector2(r.x, r.y);
				monitor.MainSize = new Vector2(r.w, r.h);
				SDL2.SDL.SDL_GetDisplayUsableBounds(n, out r);
				monitor.WorkPos = new Vector2(r.x, r.y);
				monitor.WorkSize = new Vector2(r.w, r.h);
				if (SDL2.SDL.SDL_GetDisplayDPI(n, out float dpi, out float _, out float _) != 0)
				{
					monitor.DpiScale = dpi / 96.0f;
				}
				var t = new HandleRef(plIo.Monitors, new IntPtr(0x99));
				var tt = new ImPtrVector<ImGuiPlatformMonitorPtr>(2, 2, new IntPtr(0x88), 1);
				var z = new List<ImGuiPlatformMonitorPtr>();
				z.Add(plIo.Monitors[0]);
				var ttt = Unsafe.AsRef(t.Handle);
				Unsafe.Write((void*)ttt, tt);
				//mon = monitor;
			}
		}

		private unsafe bool PlatformCreateVkSurface(ImGuiViewportPtr viewport)
		{
			ViewportDataPtr data = viewport.PlatformUserData;
			SDL_SysWMinfo sysWmInfo;
			Sdl2Native.SDL_GetVersion(&sysWmInfo.version);
			Sdl2Native.SDL_GetWMWindowInfo(data.SdlWindowHandle, &sysWmInfo);
			return GetSurfaceSource(sysWmInfo).CreateSurface(new Vulkan.VkInstance(data.SdlWindowHandle)) != Vulkan.VkSurfaceKHR.Null;
		}

		private static unsafe VkSurfaceSource GetSurfaceSource(SDL_SysWMinfo sysWmInfo)
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

		private void PlatformSwapBuffers(ImGuiViewportPtr viewport)
		{
			ViewportDataPtr data = viewport.PlatformUserData;
			if (data.GlContext != IntPtr.Zero)
			{
				Sdl2Native.SDL_GL_MakeCurrent(data.SdlWindowHandle, data.GlContext);
				Sdl2Native.SDL_GL_SwapWindow(data.SdlWindowHandle);
			}
		}

		private void PlatformRenderWindow(ImGuiViewportPtr viewport)
		{
			ViewportDataPtr data = viewport.PlatformUserData;
			if (data.GlContext != IntPtr.Zero)
				Sdl2Native.SDL_GL_MakeCurrent(data.SdlWindowHandle, data.GlContext);
		}

		private bool PlatformGetWindowMinimized(ImGuiViewportPtr viewport)
		{
			ViewportDataPtr data = viewport.PlatformUserData;
			return (Sdl2Native.SDL_GetWindowFlags(data.SdlWindowHandle) & SDL_WindowFlags.Minimized) != 0;
		}

		private bool PlatformGetWindowFocus(ImGuiViewportPtr viewport)
		{
			ViewportDataPtr data = viewport.PlatformUserData;
			return (Sdl2Native.SDL_GetWindowFlags(data.SdlWindowHandle) & SDL_WindowFlags.InputFocus) != 0;
		}

		private void PlatformSetWindowFocus(ImGuiViewportPtr viewport)
		{
			//figure out how to do this later
		}

		private void PlatformSetWindowAlpha(ImGuiViewportPtr viewport, float alpha)
		{
			ViewportDataPtr data = viewport.PlatformUserData;
			Sdl2Native.SDL_SetWindowOpacity(data.SdlWindowHandle, alpha);
		}

		private void PlatformSetWindowTitle(ImGuiViewportPtr viewport, string title)
		{
			ViewportDataPtr data = viewport.PlatformUserData;
			Sdl2Native.SDL_SetWindowTitle(data.SdlWindowHandle, title);
		}

		private void PlatformSetWindowPosition(ImGuiViewportPtr viewport, Vector2 pos)
		{
			ViewportDataPtr data = viewport.PlatformUserData;
			Sdl2Native.SDL_SetWindowPosition(data.SdlWindowHandle, (int)pos.X, (int)pos.Y);
		}

		private void PlatformSetWindowSize(ImGuiViewportPtr viewport, Vector2 size)
		{
			ViewportDataPtr data = viewport.PlatformUserData;
			Sdl2Native.SDL_SetWindowPosition(data.SdlWindowHandle, (int)size.X, (int)size.Y);
		}

		private unsafe Vector2 PlatformGetWindowSize(ImGuiViewportPtr viewport)
		{
			ViewportDataPtr data = viewport.PlatformUserData;
			int w, h;
			Sdl2Native.SDL_GetWindowSize(data.SdlWindowHandle, &w, &h);
			return new Vector2(w, h);
		}

		private unsafe Vector2 PlatformGetWindowPosition(ImGuiViewportPtr viewport)
		{
			ViewportDataPtr data = viewport.PlatformUserData;
			int x, y;
			Sdl2Native.SDL_GetWindowPosition(data.SdlWindowHandle, &x, &y);
			return new Vector2(x, y);
		}

		private unsafe void PlatformCreateWindow(ImGuiViewportPtr viewport)
		{
			ViewportDataPtr data = new ViewportDataPtr(new IntPtr(viewport.NativePtr));
			viewport.PlatformUserData = new HandleRef(data, (IntPtr)data.NativePtr).Handle;

			var t = Unsafe.AsRef<ViewportDataPtr>(viewport.PlatformUserData);

			ImGuiViewportPtr mainViewport = ImGui.GetMainViewport();
			ViewportDataPtr mainViewportData = mainViewport.PlatformUserData;

			bool useOpenGl = mainViewportData.GlContext != IntPtr.Zero;
			IntPtr glContextBackup = IntPtr.Zero;
			if (useOpenGl)
			{
				glContextBackup = Sdl2Native.SDL_GL_GetCurrentContext();
				Sdl2Native.SDL_GL_SetAttribute(SDL_GLAttribute.ShareWithCurrentContext, 1);
				Sdl2Native.SDL_GL_MakeCurrent(mainViewportData.SdlWindowHandle, mainViewportData.GlContext);
			}

			SDL_WindowFlags sdlFlags = (Sdl2Native.SDL_GetWindowFlags(data.SdlWindowHandle) & SDL_WindowFlags.AllowHighDpi) | SDL_WindowFlags.Hidden;
			sdlFlags |= (viewport.Flags & ImGuiViewportFlags.NoDecoration) != 0 ? SDL_WindowFlags.Borderless : SDL_WindowFlags.Resizable;
			if (useOpenGl)
				sdlFlags |= SDL_WindowFlags.OpenGL;
			if ((viewport.Flags & ImGuiViewportFlags.TopMost) != 0)
				sdlFlags |= SDL_WindowFlags.AlwaysOnTop;

			data.SdlWindowHandle = new Sdl2Window("Viewport", (int)viewport.Pos.X, (int)viewport.Pos.Y,
				(int)viewport.Size.X, (int)viewport.Size.Y, sdlFlags, false).SdlWindowHandle;
			data.WindowOwned = true;
			if (useOpenGl)
			{
				data.GlContext = Sdl2Native.SDL_GL_CreateContext(data.SdlWindowHandle);
				Sdl2Native.SDL_GL_SetSwapInterval(0);
				if (glContextBackup != IntPtr.Zero)
					Sdl2Native.SDL_GL_MakeCurrent(data.SdlWindowHandle, glContextBackup);
			}

			viewport.PlatformHandle = data.SdlWindowHandle;

			SDL_SysWMinfo sysWmInfo;
			Sdl2Native.SDL_GetVersion(&sysWmInfo.version);
			Sdl2Native.SDL_GetWMWindowInfo(data.SdlWindowHandle, &sysWmInfo);
			if (sysWmInfo.subsystem == SysWMType.Windows)
				viewport.PlatformHandleRaw = Unsafe.Read<Win32WindowInfo>(&sysWmInfo.info).hinstance;
		}

		private unsafe void PlatformDestroyWindow(ImGuiViewportPtr viewport)
		{
			ViewportDataPtr data = viewport.PlatformUserData;
			if ((IntPtr)data.NativePtr != IntPtr.Zero)
			{
				if (data.GlContext != IntPtr.Zero && data.WindowOwned)
					Sdl2Native.SDL_GL_DeleteContext(data.GlContext);
				if (data.SdlWindowHandle != IntPtr.Zero && data.WindowOwned)
					Sdl2Native.SDL_DestroyWindow(data.SdlWindowHandle);
				data.GlContext = IntPtr.Zero;
				data.SdlWindowHandle = IntPtr.Zero;
				ImGui.MemFree((IntPtr)data.NativePtr);
			}
			viewport.PlatformUserData = IntPtr.Zero;
			viewport.PlatformHandle = IntPtr.Zero;
		}

		private unsafe void PlatformShowWindow(ImGuiViewportPtr viewport)
		{
			ViewportDataPtr data = viewport.PlatformUserData;
			IntPtr hWnd = viewport.PlatformHandleRaw;
			if ((viewport.Flags & ImGuiViewportFlags.NoTaskBarIcon) != 0)
			{
				long exStyle = (long)GetWindowLongPtr(hWnd, GWL_EXSTYLE);
				exStyle &= ~WS_EX_APPWINDOW;
				exStyle |= WS_EX_TOOLWINDOW;
				SetWindowLongPtr(hWnd, GWL_EXSTYLE, new IntPtr(exStyle));
			}

			if ((viewport.Flags & ImGuiViewportFlags.NoFocusOnAppearing) != 0)
			{
				ShowWindow(hWnd, SW_SHOWNA);
				return;
			}

			Sdl2Native.SDL_ShowWindow(data.SdlWindowHandle);
		}

	#region Structs
	private unsafe struct ViewportData
		{
#pragma warning disable S3459 // Unassigned members should be removed
			public SDL_Window SdlWindowHandle;
			public IntPtr GlContext;
			public uint WindowID;
			public bool WindowOwned;
#pragma warning restore S3459 // Unassigned members should be removed
		}

		private unsafe struct ViewportDataPtr
		{
			public ViewportData* NativePtr { get; }
			public ViewportDataPtr(ViewportData* nativePtr) => NativePtr = nativePtr;
			public ViewportDataPtr(IntPtr nativePtr) => NativePtr = (ViewportData*)nativePtr;
			public ref SDL_Window SdlWindowHandle => ref Unsafe.AsRef<SDL_Window>(&NativePtr->SdlWindowHandle);
			public ref IntPtr GlContext => ref Unsafe.AsRef<IntPtr>(&NativePtr->GlContext);
			public ref UInt32 WindowID => ref Unsafe.AsRef<UInt32>(&NativePtr->WindowID);
			public ref bool WindowOwned => ref Unsafe.AsRef<bool>(&NativePtr->WindowOwned);

			public static implicit operator ViewportDataPtr(IntPtr nativePtr) => new ViewportDataPtr(nativePtr);
			public static implicit operator ViewportDataPtr(ViewportData* nativePtr) => new ViewportDataPtr(nativePtr);
			public static implicit operator ViewportData*(ViewportDataPtr wrappedPtr) => wrappedPtr.NativePtr;
		}

		private struct ResourceSetInfo
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
