using BootEngine.AssetsManager;
using BootEngine.Log;
using BootEngine.Renderer.Cameras;
using BootEngine.Utils.ProfilingTools;
using System;
using System.Linq;
using System.Numerics;
using Utils.Exceptions;
using Veldrid;

namespace BootEngine.Renderer
{
	public sealed class Renderer2D : Renderer<Renderer2D>, IDisposable
	{
		#region Constants
		private const int MAX_QUADS = 250000;
		#endregion

		#region Properties
		private static Scene2D CurrentScene { get; set; }
		private readonly static GraphicsDevice _gd = Application.App.Window.GraphicsDevice;
		private readonly InstanceVertexInfo[] _instanceList = new InstanceVertexInfo[MAX_QUADS];
		private int instanceCount;

		public int InstanceCount => CurrentScene.RenderableList.Count;
		#endregion

		#region Constructor
		static Renderer2D()
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(typeof(Renderer2D));
#endif
			CurrentScene = new Scene2D();

			ResourceFactory factory = _gd.ResourceFactory;

			Span<ushort> quadIndices = stackalloc ushort[] { 0, 1, 2, 3 };
			Span<VertexPositionTexture> quadVertices = stackalloc VertexPositionTexture[]
			{
				new VertexPositionTexture(new Vector3(-.5f, .5f, 0f), new Vector2(0.0f, 1.0f)),
				new VertexPositionTexture(new Vector3(.5f, .5f, 0f), new Vector2(1.0f, 1.0f)),
				new VertexPositionTexture(new Vector3(-.5f, -.5f, 0f), new Vector2(0.0f, 0.0f)),
				new VertexPositionTexture(new Vector3(.5f, -.5f, 0f), new Vector2(1.0f, 0.0f))
			};

			BufferDescription vbDescription = new BufferDescription(
				VertexPositionTexture.SizeInBytes * 4,
				BufferUsage.VertexBuffer);
			BufferDescription ibDescription = new BufferDescription(
				4 * sizeof(ushort),
				BufferUsage.IndexBuffer);

			CurrentScene.IndexBuffer = factory.CreateBuffer(ibDescription);
			CurrentScene.VertexBuffer = factory.CreateBuffer(vbDescription);
			_gd.UpdateBuffer(CurrentScene.IndexBuffer, 0, quadIndices.ToArray());
			_gd.UpdateBuffer(CurrentScene.VertexBuffer, 0, quadVertices.ToArray());

			Scene2D.WhiteTexture = factory.CreateTexture(TextureDescription.Texture2D(
				1u, // Width
				1u, // Height
				1,  // Miplevel
				1,  // ArrayLayers
				PixelFormat.R8_G8_B8_A8_UNorm,
				TextureUsage.Sampled));
			uint[] whiteTexture = { 0xffffffff };
			_gd.UpdateTexture(
				Scene2D.WhiteTexture,
				whiteTexture,
				0,  // x
				0,  // y
				0,  // z
				1u, // Width
				1u, // Height
				1,  // Depth
				0,  // Miplevel
				0); // ArrayLayers

			CurrentScene.InstancesVertexBuffer = factory.CreateBuffer(new BufferDescription(InstanceVertexInfo.Size * MAX_QUADS, BufferUsage.VertexBuffer));
			CurrentScene.CameraBuffer = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));
			CurrentScene.Shaders = AssetManager.GenerateShadersFromFile("Texture2D.glsl");

			VertexLayoutDescription sharedVertexLayout = new VertexLayoutDescription(
				new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
				new VertexElementDescription("TexCoord", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2));

			VertexLayoutDescription instanceVertexLayout = new VertexLayoutDescription(
				new VertexElementDescription("InstancePosition", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
				new VertexElementDescription("InstanceScale", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
				new VertexElementDescription("InstanceRotation", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float1),
				new VertexElementDescription("InstanceColor", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4));
			instanceVertexLayout.InstanceStepRate = 1;

			CurrentScene.ResourceLayout = factory.CreateResourceLayout(
				new ResourceLayoutDescription(
					new ResourceLayoutElementDescription("ViewProjection", ResourceKind.UniformBuffer, ShaderStages.Vertex),
					new ResourceLayoutElementDescription("Texture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
					new ResourceLayoutElementDescription("Sampler", ResourceKind.Sampler, ShaderStages.Fragment)));

			GraphicsPipelineDescription pipelineDescription = new GraphicsPipelineDescription();
			pipelineDescription.BlendState = BlendStateDescription.SingleAlphaBlend;
			pipelineDescription.DepthStencilState = new DepthStencilStateDescription(
				depthTestEnabled: true,
				depthWriteEnabled: true,
				comparisonKind: ComparisonKind.LessEqual);
			pipelineDescription.RasterizerState = new RasterizerStateDescription(
				cullMode: FaceCullMode.Back,
				fillMode: PolygonFillMode.Solid,
				frontFace: FrontFace.Clockwise,
				depthClipEnabled: true,
				scissorTestEnabled: false);
			pipelineDescription.PrimitiveTopology = PrimitiveTopology.TriangleStrip;
			pipelineDescription.ResourceLayouts = new ResourceLayout[] { CurrentScene.ResourceLayout };
			pipelineDescription.ShaderSet = new ShaderSetDescription(
				vertexLayouts: new VertexLayoutDescription[] { sharedVertexLayout, instanceVertexLayout },
				shaders: CurrentScene.Shaders);
			pipelineDescription.Outputs = _gd.MainSwapchain.Framebuffer.OutputDescription;

			CurrentScene.Pipeline = factory.CreateGraphicsPipeline(pipelineDescription);

			CurrentScene.ResourceSetsPerTexture.Add(Scene2D.WhiteTexture, _gd.ResourceFactory.CreateResourceSet(new ResourceSetDescription(
				CurrentScene.ResourceLayout,
				CurrentScene.CameraBuffer,
				Scene2D.WhiteTexture,
				_gd.LinearSampler)));

			CurrentScene.InstancesPerTexture.Add(Scene2D.WhiteTexture, 0);
		}
		#endregion

		public static void SetCurrentScene(Scene2D scene)
		{
			CurrentScene = scene;
		}

		public void BeginScene(OrthoCamera camera)
		{
			_gd.UpdateBuffer(CurrentScene.CameraBuffer, 0, camera.ViewProjectionMatrix);
		}

		public void EndScene()
		{
			//
		}

		#region Primitives
		public Renderable2D SubmitQuadDraw(Renderable2DParameters parameters) => SubmitQuadDraw(ref parameters);

		public Renderable2D SubmitQuadDraw(ref Renderable2DParameters parameters)
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			Renderable2D renderable;
			if (parameters.Texture == null)
				renderable =  SetupInstancedQuad(ref parameters);
			else
				renderable =  SetupInstancedTextureQuad(ref parameters);

			CurrentScene.RenderableList.Add(renderable);
			return renderable;
		}

		internal Renderable2D SetupQuad(ref Renderable2DParameters parameters)
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			Renderable2D renderable = new Renderable2D(ref parameters);

			renderable.ColorBuffer = _gd.ResourceFactory.CreateBuffer(new BufferDescription(16, BufferUsage.UniformBuffer));
			_gd.UpdateBuffer(renderable.ColorBuffer, 0, parameters.Color);

			renderable.TransformBuffer = _gd.ResourceFactory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));
			Matrix4x4 translation = Matrix4x4.CreateTranslation(parameters.Position) * Matrix4x4.CreateScale(new Vector3(parameters.Size, 1f));
			if (parameters.Rotation != 0)
				translation *= Matrix4x4.CreateRotationZ(parameters.Rotation);
			_gd.UpdateBuffer(renderable.TransformBuffer, 0, translation);

			CurrentScene.InstancesPerTexture[Scene2D.WhiteTexture]++;

			return renderable;
		}

		internal Renderable2D SetupInstancedQuad(ref Renderable2DParameters parameters)
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			CurrentScene.InstancesPerTexture[Scene2D.WhiteTexture]++;

			_instanceList[instanceCount] = new InstanceVertexInfo(parameters.Position, parameters.Size, parameters.Rotation, parameters.Color);
			instanceCount++;

			return new Renderable2D(ref parameters, instanceCount - 1);
		}

		internal Renderable2D SetupTextureQuad(ref Renderable2DParameters parameters)
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			Renderable2D renderable = new Renderable2D(ref parameters);

			renderable.ColorBuffer = _gd.ResourceFactory.CreateBuffer(new BufferDescription(16, BufferUsage.UniformBuffer));
			_gd.UpdateBuffer(renderable.ColorBuffer, 0, parameters.Color);

			renderable.TransformBuffer = _gd.ResourceFactory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));
			Matrix4x4 translation = Matrix4x4.CreateTranslation(parameters.Position) * Matrix4x4.CreateScale(new Vector3(parameters.Size, 1f));
			if (parameters.Rotation != 0)
				translation *= Matrix4x4.CreateRotationZ(parameters.Rotation);
			_gd.UpdateBuffer(renderable.TransformBuffer, 0, translation);

			renderable.Texture = parameters.Texture;

			if (!CurrentScene.ResourceSetsPerTexture.ContainsKey(parameters.Texture))
			{
				CurrentScene.ResourceSetsPerTexture.Add(renderable.Texture, _gd.ResourceFactory.CreateResourceSet(new ResourceSetDescription(
					CurrentScene.ResourceLayout,
					CurrentScene.CameraBuffer,
					renderable.Texture,
					_gd.LinearSampler)));
				CurrentScene.InstancesPerTexture.Add(parameters.Texture, 1);
			}
			else
			{
				CurrentScene.InstancesPerTexture[parameters.Texture]++;
			}

			return renderable;
		}

		internal Renderable2D SetupInstancedTextureQuad(ref Renderable2DParameters parameters)
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			Renderable2D renderable = new Renderable2D(ref parameters, instanceCount);

			if (!CurrentScene.ResourceSetsPerTexture.ContainsKey(parameters.Texture))
			{
				CurrentScene.ResourceSetsPerTexture.Add(parameters.Texture, _gd.ResourceFactory.CreateResourceSet(new ResourceSetDescription(
					CurrentScene.ResourceLayout,
					CurrentScene.CameraBuffer,
					renderable.Texture,
					_gd.LinearSampler)));
				CurrentScene.InstancesPerTexture.Add(parameters.Texture, 1);
			}
			else
			{
				CurrentScene.InstancesPerTexture[parameters.Texture]++;
			}

			_instanceList[instanceCount] = new InstanceVertexInfo(parameters.Position, parameters.Size, parameters.Rotation, parameters.Color);
			instanceCount++;

			return renderable;
		}

		public void RemoveQuadDraw(int index)
		{
			Renderable2D renderable = GetRenderableByIndex(index);
			CurrentScene.RenderableList.RemoveAt(index);
			CurrentScene.InstancesPerTexture[renderable.Texture ?? Scene2D.WhiteTexture]--;
			instanceCount--;
			renderable.Dispose();
		}
		#endregion

		internal void UpdateBuffer<T>(DeviceBuffer buffer, T value) where T : struct
		{
			_gd.UpdateBuffer(buffer, 0, value);
		}

		public void UpdatePosition(string renderableName, Vector3 position) => UpdateTransform(renderableName, position, null, null);
		public void UpdatePosition(int index, Vector3 position) => UpdateTransform(index, position, null, null);
		public void UpdateSize(string renderableName, Vector2 size) => UpdateTransform(renderableName, null, size, null);
		public void UpdateSize(int index, Vector2 size) => UpdateTransform(index, null, size, null);
		public void UpdateRotation(string renderableName, float rotation) => UpdateTransform(renderableName, null, null, rotation);
		public void UpdateRotation(int index, float rotation) => UpdateTransform(index, null, null, rotation);

		public void UpdateTransform(string renderableName, Vector3? position = null, Vector2? size = null, float? rotation = null) => UpdateTransform(GetRenderableByName(renderableName), position, size, rotation);

		public void UpdateTransform(int index, Vector3? position = null, Vector2? size = null, float? rotation = null) => UpdateTransform(GetRenderableByIndex(index), position, size, rotation);

		public void UpdateTransform(Renderable2D renderable, Vector3? position = null, Vector2? size = null, float? rotation = null, bool instancedDraw = true)
		{
			if (!instancedDraw)
			{
				if (position.HasValue)
					renderable.Position = position.Value;
				if (size.HasValue)
					renderable.Size = size.Value;
				if (rotation.HasValue)
					renderable.Rotation = rotation.Value;

				Matrix4x4 translation = Matrix4x4.CreateTranslation(renderable.Position)
					* Matrix4x4.CreateScale(new Vector3(renderable.Size, 1f));
				if (renderable.Rotation != 0)
					translation *= Matrix4x4.CreateRotationZ(renderable.Rotation);

				UpdateBuffer(renderable.TransformBuffer, translation);
			}
			else
			{
				if (!renderable.InstaceIndex.HasValue)
					throw new BootEngineException("Renderable object not drawn via instancing. Use instancedDraw = false instead.");
				int index = renderable.InstaceIndex.Value;
				if (position.HasValue)
				{
					_instanceList[index].Position = position.Value;
					renderable.Position = position.Value;
				}
				if (size.HasValue)
				{
					_instanceList[index].Scale = size.Value;
					renderable.Size = size.Value;
				}
				if (rotation.HasValue)
				{
					_instanceList[index].Rotation = rotation.Value;
					renderable.Rotation = rotation.Value;
				}
			}
		}

		public void UpdateColor(string renderableName, Vector4 value)
		{
			UpdateColor(GetRenderableByName(renderableName).InstaceIndex.Value, value);
		}

		public void UpdateColor(int index, Vector4 value)
		{
			_instanceList[index].Color = value;
			GetRenderableByIndex(index).Color = value;
		}

		public Renderable2D GetRenderableByName(string name)
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			return CurrentScene.RenderableList.Find(r => r.Name == name) as Renderable2D;
		}

		public Renderable2D GetRenderableByIndex(int index)
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			return CurrentScene.RenderableList[index] as Renderable2D;
		}

		public void Render()
		{
			Render(CurrentScene, true);
		}

		protected override void BeginRender(CommandList cl)
		{
			cl.Begin();
			cl.SetFramebuffer(_gd.SwapchainFramebuffer);
			cl.SetViewport(0, new Viewport(0, 0, _gd.SwapchainFramebuffer.Width, _gd.SwapchainFramebuffer.Height, 0, 1));
			cl.SetFullViewports();
			cl.ClearColorTarget(0, RgbaFloat.Grey);
			cl.ClearDepthStencil(1f);
			cl.SetVertexBuffer(0, CurrentScene.VertexBuffer);
			cl.SetIndexBuffer(CurrentScene.IndexBuffer, IndexFormat.UInt16);
			cl.SetPipeline(CurrentScene.Pipeline);
		}

		protected override void InnerRender(Renderable renderable, CommandList cl)
		{
			Logger.Assert(renderable is Renderable2D, "Renderable object should be of type " + nameof(Renderable2D));

			Renderable2D renderable2d = renderable as Renderable2D;

			//cl.SetGraphicsResourceSet(0, renderable2d.ResourceSet);
			cl.DrawIndexed(
				indexCount: 4,
				instanceCount: 1,
				indexStart: 0,
				vertexOffset: 0,
				instanceStart: 0);
		}

		protected override void BatchRender(CommandList cl)
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			var ordered = CurrentScene.RenderableList.OrderBy(r => (r as Renderable2D)?.Texture).ToList();
			for (int i = 0; i < CurrentScene.RenderableList.Count; i++)
			{
				var t = ordered[i] as Renderable2D;
				t.InstaceIndex = i;
				_instanceList[i].Position = t.Position;
				_instanceList[i].Scale = t.Size;
				_instanceList[i].Rotation = t.Rotation;
				_instanceList[i].Color = t.Color;
			}
			cl.UpdateBuffer(CurrentScene.InstancesVertexBuffer, 0, _instanceList);
			cl.SetVertexBuffer(1, CurrentScene.InstancesVertexBuffer);
			uint instanceStart = 0;
			foreach (var entry in CurrentScene.ResourceSetsPerTexture)
			{
				uint instancePerTexCount = CurrentScene.InstancesPerTexture[entry.Key];
				cl.SetGraphicsResourceSet(0, entry.Value);
				cl.DrawIndexed(
					indexCount: 4,
					instanceCount: instancePerTexCount,
					indexStart: 0,
					vertexOffset: 0,
					instanceStart: instanceStart);
				instanceStart += instancePerTexCount;
			}
		}

		protected override void EndRender(CommandList cl)
		{
			cl.End();
			_gd.SubmitCommands(cl);
		}

		public void Dispose()
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			CurrentScene.Dispose();
		}

		private readonly struct VertexPositionTexture
		{
			public const uint SizeInBytes = 20;
			public readonly Vector3 Position { get; }
			public readonly Vector2 TexCoord { get; }

			public VertexPositionTexture(Vector3 position, Vector2 texCoord)
			{
				this.Position = position;
				this.TexCoord = texCoord;
			}
		}

		private struct InstanceVertexInfo
		{
			public static uint Size { get; } = (uint)System.Runtime.CompilerServices.Unsafe.SizeOf<InstanceVertexInfo>();

			public Vector3 Position;
			public Vector2 Scale;
			public float Rotation;
			public Vector4 Color;

			public InstanceVertexInfo(Vector3 position, Vector2 scale, float rotation, Vector4 color)
			{
				Position = position;
				Scale = scale;
				Rotation = rotation;
				Color = color;
			}
		}
	}

	public ref struct Renderable2DParameters
	{
		public Renderable2DParameters(Vector2? position, Vector2? size, float? rotation, Vector4? color = null, Texture texture = null)
		{
			Name = null;
			Position = new Vector3(position ?? Vector2.Zero, 0f);
			Size = size ?? Vector2.One;
			Rotation = rotation ?? 0f;
			Color = color ?? RgbaFloat.White.ToVector4();
			Texture = texture ?? Scene2D.WhiteTexture;
		}

		public Renderable2DParameters(Vector3? position = null, Vector2? size = null, float? rotation = null, Vector4? color = null, Texture texture = null)
		{
			Name = null;
			Position = position ?? Vector3.Zero;
			Size = size ?? Vector2.One;
			Rotation = rotation ?? 0f;
			Color = color ?? RgbaFloat.White.ToVector4();
			Texture = texture ?? Scene2D.WhiteTexture;
		}

		public Renderable2DParameters(string name, Vector3? position, Vector2? size, float? rotation, Vector4? color = null, Texture texture = null)
		{
			Name = name;
			Position = position ?? Vector3.Zero;
			Size = size ?? Vector2.One;
			Rotation = rotation ?? 0f;
			Color = color ?? RgbaFloat.White.ToVector4();
			Texture = texture ?? Scene2D.WhiteTexture;
		}

		public string Name { get; set; }
		public Vector4 Color { get; set; }
		public Vector3 Position { get; set; }
		public Vector2 Size { get; set; }
		public float Rotation { get; set; }
		public Texture Texture { get; set; }
	}
}
