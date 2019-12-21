using BootEngine.Renderer.Cameras;
using System;
using System.Numerics;
using Veldrid;
using BootEngine.AssetsManager;
using BootEngine.Log;

namespace BootEngine.Renderer
{
	public sealed class Renderer2D : Renderer<Renderer2D>, IDisposable
	{
		#region Propriedades
		private static Scene2D Scene { get; }
		private readonly static GraphicsDevice _gd = Application.App.Window.GraphicsDevice;
		#endregion

		#region Construtor
		static Renderer2D()
		{
			Scene = new Scene2D();

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

			Scene.IndexBuffer = factory.CreateBuffer(ibDescription);
			Scene.VertexBuffer = factory.CreateBuffer(vbDescription);
			_gd.UpdateBuffer(Scene.IndexBuffer, 0, quadIndices.ToArray());
			_gd.UpdateBuffer(Scene.VertexBuffer, 0, quadVertices.ToArray());

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

			Scene.CameraBuffer = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));
			Scene.Shaders = AssetManager.GenerateShadersFromFile("Texture2D.glsl");

			VertexLayoutDescription vertexLayout = new VertexLayoutDescription(
				new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
				new VertexElementDescription("TexCoord", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2));

			Scene.ResourceLayout = factory.CreateResourceLayout(
				new ResourceLayoutDescription(
					new ResourceLayoutElementDescription("ViewProjection", ResourceKind.UniformBuffer, ShaderStages.Vertex),
					new ResourceLayoutElementDescription("Transform", ResourceKind.UniformBuffer, ShaderStages.Vertex),
					new ResourceLayoutElementDescription("Color", ResourceKind.UniformBuffer, ShaderStages.Fragment),
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
			pipelineDescription.ResourceLayouts = new ResourceLayout[] { Scene.ResourceLayout };
			pipelineDescription.ShaderSet = new ShaderSetDescription(
				vertexLayouts: new VertexLayoutDescription[] { vertexLayout },
				shaders: Scene.Shaders);
			pipelineDescription.Outputs = _gd.MainSwapchain.Framebuffer.OutputDescription;

			Scene.Pipeline = factory.CreateGraphicsPipeline(pipelineDescription);

		}
		#endregion

		public void BeginScene(OrthoCamera camera)
		{
			_gd.UpdateBuffer(Scene.CameraBuffer, 0, camera.ViewProjectionMatrix);
		}

		public void EndScene()
		{
			//
		}

		#region Primitives
		public Renderable2D SubmitQuadDraw(Vector2 position, Vector2 size, Vector4 color)
		{
			return SubmitQuadDraw(new Vector3(position, 0f), size, color);
		}

		public Renderable2D SubmitQuadDraw(Rectangle rect, Vector4 color)
		{
			return SubmitQuadDraw(new Vector2(rect.X, rect.Y), new Vector2(rect.Width, rect.Height), color);
		}

		public Renderable2D SubmitQuadDraw(Vector3 position, Vector2 size, Vector4 color)
		{
			Renderable2D renderable = new Renderable2D();

			renderable.ColorBuffer = _gd.ResourceFactory.CreateBuffer(new BufferDescription(16, BufferUsage.UniformBuffer));
			_gd.UpdateBuffer(renderable.ColorBuffer, 0, color);

			renderable.TransformBuffer = _gd.ResourceFactory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));
			Matrix4x4 translation = Matrix4x4.CreateTranslation(position) * Matrix4x4.CreateScale(new Vector3(size, 1f));
			_gd.UpdateBuffer(renderable.TransformBuffer, 0, translation);

			renderable.ResourceSet = _gd.ResourceFactory.CreateResourceSet(new ResourceSetDescription(
				Scene.ResourceLayout,
				Scene.CameraBuffer,
				renderable.TransformBuffer,
				renderable.ColorBuffer,
				Scene2D.WhiteTexture,
				_gd.LinearSampler));

			Scene.RenderableList.Add(renderable);

			return renderable;
		}

		public Renderable2D SubmitTexture(Vector3 position, Vector2 size, Texture texture)
		{
			Renderable2D renderable = new Renderable2D();

			renderable.ColorBuffer = _gd.ResourceFactory.CreateBuffer(new BufferDescription(16, BufferUsage.UniformBuffer));
			_gd.UpdateBuffer(renderable.ColorBuffer, 0, RgbaFloat.White);

			renderable.TransformBuffer = _gd.ResourceFactory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));
			Matrix4x4 translation = Matrix4x4.CreateTranslation(position) * Matrix4x4.CreateScale(new Vector3(size, 1f));
			_gd.UpdateBuffer(renderable.TransformBuffer, 0, translation);

			renderable.Texture = texture;

			renderable.ResourceSet = _gd.ResourceFactory.CreateResourceSet(new ResourceSetDescription(
				Scene.ResourceLayout,
				Scene.CameraBuffer,
				renderable.TransformBuffer,
				renderable.ColorBuffer,
				renderable.Texture,
				_gd.LinearSampler));

			Scene.RenderableList.Add(renderable);

			return renderable;
		}
		#endregion

		public void UpdateBuffer<T>(DeviceBuffer buffer, T value) where T : struct
		{
			_gd.UpdateBuffer(buffer, 0, value);
		}

		public void Render()
		{
			Render(Scene);
		}

		protected override void BeginRender(CommandList cl)
		{
			cl.Begin();
			cl.SetFramebuffer(_gd.SwapchainFramebuffer);
			cl.SetViewport(0, new Viewport(0, 0, _gd.SwapchainFramebuffer.Width, _gd.SwapchainFramebuffer.Height, 0, 1));
			cl.SetFullViewports();
			cl.ClearColorTarget(0, RgbaFloat.Grey);
			cl.ClearDepthStencil(1f);
			cl.SetVertexBuffer(0, Scene.VertexBuffer);
			cl.SetIndexBuffer(Scene.IndexBuffer, IndexFormat.UInt16);
			cl.SetPipeline(Scene.Pipeline);
		}

		protected override void InnerRender(Renderable renderable, CommandList cl)
		{
			Logger.Assert(renderable is Renderable2D, "Renderable object should be of type " + nameof(Renderable2D));

			Renderable2D renderable2d = renderable as Renderable2D;

			cl.SetGraphicsResourceSet(0, renderable2d.ResourceSet);
			cl.DrawIndexed(
				indexCount: 4,
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
			Scene.Dispose();
		}

		#region Struct
		private readonly struct VertexPositionTexture
		{
			public const uint SizeInBytes = 20;
			public readonly Vector3 Position;
			public readonly Vector2 TexCoord;

			public VertexPositionTexture(Vector3 position, Vector2 texCoord)
			{
				this.Position = position;
				this.TexCoord = texCoord;
			}
		}
		#endregion
	}
}
