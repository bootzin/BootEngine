using BootEngine.Input;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Utils;
using Utils.Exceptions;
using Veldrid;

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
        public ImGuiController(GraphicsDevice gd, OutputDescription outputDescription, int width, int height)
        {
            windowWidth = width;
            windowHeight = height;

            ImGui.SetCurrentContext(ImGui.CreateContext());
            ImGui.GetIO().Fonts.AddFontDefault();

            CreateDeviceResources(gd, outputDescription);
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

            io.KeyCtrl = io.KeysDown[(int)Key.ControlLeft] || io.KeysDown[(int)Key.ControlRight];
            io.KeyAlt = io.KeysDown[(int)Key.AltLeft] || io.KeysDown[(int)Key.AltRight];
            io.KeyShift = io.KeysDown[(int)Key.ShiftLeft] || io.KeysDown[(int)Key.ShiftRight];
            io.KeySuper = io.KeysDown[(int)Key.WinLeft] || io.KeysDown[(int)Key.WinRight];
            #endregion
        }

        private static void SetImGuiKeyMappings()
        {
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
            io.KeyMap[(int)ImGuiKey.Delete] = (int)KeyCodes.Delete;
            io.KeyMap[(int)ImGuiKey.Backspace] = (int)KeyCodes.BackSpace;
            io.KeyMap[(int)ImGuiKey.Enter] = (int)KeyCodes.Enter;
            io.KeyMap[(int)ImGuiKey.Escape] = (int)KeyCodes.Escape;
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
    }
}
