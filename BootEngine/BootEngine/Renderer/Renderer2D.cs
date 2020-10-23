using BootEngine.Renderer.Cameras;
using System;
using System.Numerics;
using Veldrid;
using BootEngine.AssetsManager;
using BootEngine.Log;
using BootEngine.Utils.ProfilingTools;

namespace BootEngine.Renderer
{
	public sealed class Renderer2D : Renderer<Renderer2D>, IDisposable
	{
		#region Properties
		private static Scene2D CurrentScene { get; set; }
		private readonly static GraphicsDevice _gd = Application.App.Window.GraphicsDevice;
		private readonly static CommandList CommandList = _gd.ResourceFactory.CreateCommandList();
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

			BufferDescription quadVb = new BufferDescription(
				QuadVertex.SizeInBytes * Scene2D.MaxVertices,
				BufferUsage.VertexBuffer | BufferUsage.Dynamic);
			BufferDescription quadIb = new BufferDescription(
				sizeof(ushort) * Scene2D.MaxIndices,
				BufferUsage.IndexBuffer);

			CurrentScene.IndexBuffer = factory.CreateBuffer(quadIb);
			CurrentScene.VertexBuffer = factory.CreateBuffer(quadVb);

			CurrentScene.QuadVertexPositions = new Vector3[]
			{
				new Vector3(-.5f, .5f, 0f),
				new Vector3(.5f, .5f, 0f),
				new Vector3(-.5f, -.5f, 0f),
				new Vector3(.5f, -.5f, 0f)
			};

			CurrentScene.QuadTexCoords = new Vector2[]
			{
				new Vector2(0, 1),
				new Vector2(1, 1),
				new Vector2(1, 0),
				new Vector2(0, 0)
			};

			Span<ushort> quadIndices = stackalloc ushort[Scene2D.MaxIndices];

			ushort offset = 0;
			for (int i = 0; i < Scene2D.MaxIndices; i+=6)
			{
				quadIndices[i] = offset;
				quadIndices[i + 1] = (ushort)(offset + 1);
				quadIndices[i + 2] = (ushort)(offset + 2);

				quadIndices[i + 3] = (ushort)(offset + 1);
				quadIndices[i + 4] = (ushort)(offset + 3);
				quadIndices[i + 5] = (ushort)(offset + 2);

				offset += 4;
			}

			_gd.UpdateBuffer(CurrentScene.IndexBuffer, 0, quadIndices.ToArray());

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

			CurrentScene.CameraBuffer = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));
			CurrentScene.Shaders = AssetManager.GenerateShadersFromFile("Batch2D.glsl");

			VertexLayoutDescription vertexLayout = new VertexLayoutDescription(
				new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
				new VertexElementDescription("TexCoord", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
				new VertexElementDescription("Color", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4),
				new VertexElementDescription("TexIndex", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float1),
				new VertexElementDescription("TilingFactor", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float1));

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
			pipelineDescription.PrimitiveTopology = PrimitiveTopology.TriangleList;
			pipelineDescription.ResourceLayouts = new ResourceLayout[] { CurrentScene.ResourceLayout };
			pipelineDescription.ShaderSet = new ShaderSetDescription(
				vertexLayouts: new VertexLayoutDescription[] { vertexLayout },
				shaders: CurrentScene.Shaders);
			pipelineDescription.Outputs = _gd.MainSwapchain.Framebuffer.OutputDescription;

			CurrentScene.Pipeline = factory.CreateGraphicsPipeline(pipelineDescription);
		}
		#endregion

		public static void SetCurrentScene(Scene2D scene)
		{
			CurrentScene = scene;
		}

		public void BeginScene(OrthoCamera camera)
		{
			_gd.UpdateBuffer(CurrentScene.CameraBuffer, 0, camera.ViewProjectionMatrix);

			StartBatch();
		}

		private void StartBatch()
		{
			CurrentScene.IndexCount = 0;
			CurrentScene.CurrentQuadVertex = 0;
			CurrentScene.QuadVertexBufferBase = new QuadVertex[Scene2D.MaxVertices];
		}

		public void EndScene()
		{
			//
		}

		private void NextBatch()
		{
			BatchRender(CommandList);
			StartBatch();
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
				renderable =  SetupQuad(parameters.Position, parameters.Size, parameters.Rotation, parameters.Color);
			else
				renderable =  SetupTextureQuad(parameters.Position, parameters.Size, parameters.Rotation, parameters.Color, parameters.Texture);

			renderable.SetParameters(ref parameters);
			CurrentScene.RenderableList.Add(renderable);
			return renderable;
		}

		internal Renderable2D SetupQuad(Vector3 position, Vector2 size, float rotation, Vector4 color)
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			Renderable2D renderable = new Renderable2D();

			Matrix4x4 translation = Matrix4x4.CreateTranslation(position) * Matrix4x4.CreateScale(new Vector3(size, 1f));
			if (rotation != 0)
				translation *= Matrix4x4.CreateRotationZ(rotation);

			renderable.Translation = translation;

			renderable.Color = color;

			renderable.ResourceSet = _gd.ResourceFactory.CreateResourceSet(new ResourceSetDescription(
				CurrentScene.ResourceLayout,
				CurrentScene.CameraBuffer,
				Scene2D.WhiteTexture,
				_gd.LinearSampler));

			return renderable;
		}

		internal Renderable2D SetupTextureQuad(Vector3 position, Vector2 size, float rotation, Vector4 color, Texture texture)
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			Renderable2D renderable = new Renderable2D();

			Matrix4x4 translation = Matrix4x4.CreateTranslation(position) * Matrix4x4.CreateScale(new Vector3(size, 1f));
			if (rotation != 0)
				translation *= Matrix4x4.CreateRotationZ(rotation);

			renderable.Translation = translation;

			renderable.Color = color;

			renderable.Texture = texture;

			renderable.ResourceSet = _gd.ResourceFactory.CreateResourceSet(new ResourceSetDescription(
				CurrentScene.ResourceLayout,
				CurrentScene.CameraBuffer,
				renderable.Texture,
				_gd.LinearSampler));

			return renderable;
		}

		public void RemoveQuadDraw(int index)
		{
			GetRenderableByIndex(index).Dispose();
			CurrentScene.RenderableList.RemoveAt(index);
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

		public void UpdateTransform(Renderable2D renderable, Vector3? position = null, Vector2? size = null, float? rotation = null)
		{
			if (position.HasValue)
				renderable.Position = position.Value;
			if (size.HasValue)
				renderable.Size = size.Value;
			if (rotation.HasValue)
				renderable.Rotation = rotation.Value;

			Matrix4x4 model = Matrix4x4.CreateScale(new Vector3(renderable.Size, 1f));
			if (renderable.Rotation != 0)
				model *= Matrix4x4.CreateRotationZ(renderable.Rotation);
			model *= Matrix4x4.CreateTranslation(renderable.Position);

			//UpdateBuffer(renderable.TransformBuffer, model);
		}

		public void UpdateColor(string renderableName, Vector4 value)
		{
			Renderable2D renderable = GetRenderableByName(renderableName);
			//UpdateBuffer(renderable.ColorBuffer, value);
		}

		public void UpdateColor(int index, Vector4 value)
		{
			Renderable2D renderable = GetRenderableByIndex(index);
			//UpdateBuffer(renderable.ColorBuffer, value);
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
			Render(CurrentScene, CommandList);
		}

		protected override void BeginRender(CommandList cl)
		{
			cl.Begin();
			cl.SetFramebuffer(_gd.SwapchainFramebuffer);
			cl.SetFullViewport(0);
			cl.ClearColorTarget(0, RgbaFloat.Grey);
			cl.ClearDepthStencil(1f);
			cl.SetVertexBuffer(0, CurrentScene.VertexBuffer);
			cl.SetIndexBuffer(CurrentScene.IndexBuffer, IndexFormat.UInt16);
			cl.SetPipeline(CurrentScene.Pipeline);

			StartBatch();

			CurrentScene.RenderableList.ForEach(renderable =>
			{
				var r2d = renderable as Renderable2D;
				for (int i = 0; i < 4; i++)
				{
					CurrentScene.QuadVertexBufferBase[CurrentScene.CurrentQuadVertex++] = new QuadVertex(
						Vector3.Transform(CurrentScene.QuadVertexPositions[i], r2d.Translation),
						CurrentScene.QuadTexCoords[i],
						r2d.Color,
						0,
						1);
				}

				CurrentScene.IndexCount += 6;

				if (CurrentScene.IndexCount >= Scene2D.MaxIndices)
				{
					NextBatch();
				}
			});
		}

		protected override void BatchRender(CommandList cl)
		{
			if (CurrentScene.IndexCount == 0)
				return;

			_gd.UpdateBuffer(CurrentScene.VertexBuffer, 0, CurrentScene.QuadVertexBufferBase);

			cl.SetGraphicsResourceSet(0, (CurrentScene.RenderableList[0] as Renderable2D).ResourceSet);
			cl.DrawIndexed(
				indexCount: CurrentScene.IndexCount,
				instanceCount: 1,
				indexStart: 0,
				vertexOffset: 0,
				instanceStart: 0);
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
	}

	public readonly struct QuadVertex
	{
		public const uint SizeInBytes = 44;
		public readonly Vector3 Position { get; }
		public readonly Vector2 TexCoord { get; }
		public readonly Vector4 Color { get; }
		public readonly float TexIndex { get; }
		public float TilingFactor { get; }

		public QuadVertex(Vector3 position, Vector2 texCoord, Vector4 color, float texIndex, float tilingFactor)
		{
			Position = position;
			TexCoord = texCoord;
			Color = color;
			TexIndex = texIndex;
			TilingFactor = tilingFactor;
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
