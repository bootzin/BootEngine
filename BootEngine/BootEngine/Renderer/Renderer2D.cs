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

			var factory = _gd.ResourceFactory;

			Span<ushort> quadIndices = stackalloc ushort[] { 0, 1, 2, 3 };
			Span<Vector2> quadVertices = stackalloc Vector2[]
			{
				new Vector2(-.5f, .5f),
				new Vector2(.5f, .5f),
				new Vector2(-.5f, -.5f),
				new Vector2(.5f, -.5f),
			};

			VertexLayoutDescription vertexLayout = new VertexLayoutDescription(
				new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
				new VertexElementDescription("TexCoord", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2));

			BufferDescription vbDescription = new BufferDescription(
				32,
				BufferUsage.VertexBuffer);
			BufferDescription ibDescription = new BufferDescription(
				4 * sizeof(ushort),
				BufferUsage.IndexBuffer);

			Scene.CameraBuffer = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));

			Scene.IndexBuffer = factory.CreateBuffer(ibDescription);
			Scene.VertexBuffer = factory.CreateBuffer(vbDescription);
			_gd.UpdateBuffer(Scene.IndexBuffer, 0, quadIndices.ToArray());
			_gd.UpdateBuffer(Scene.VertexBuffer, 0, quadVertices.ToArray());

			Scene.Shaders = AssetManager.GenerateShadersFromFile("FlatColor.glsl");

			ResourceLayout resourceLayout = factory.CreateResourceLayout(
				new ResourceLayoutDescription(
					new ResourceLayoutElementDescription("ViewProjection", ResourceKind.UniformBuffer, ShaderStages.Vertex),
					new ResourceLayoutElementDescription("Transform", ResourceKind.UniformBuffer, ShaderStages.Vertex),
					new ResourceLayoutElementDescription("Color", ResourceKind.UniformBuffer, ShaderStages.Fragment)));
			_gd.DisposeWhenIdle(resourceLayout);

			GraphicsPipelineDescription pipelineDescription = new GraphicsPipelineDescription();
			pipelineDescription.BlendState = BlendStateDescription.SingleOverrideBlend;
			pipelineDescription.DepthStencilState = new DepthStencilStateDescription(
				depthTestEnabled: false,
				depthWriteEnabled: true,
				comparisonKind: ComparisonKind.Always);
			pipelineDescription.RasterizerState = new RasterizerStateDescription(
				cullMode: FaceCullMode.Back,
				fillMode: PolygonFillMode.Solid,
				frontFace: FrontFace.Clockwise,
				depthClipEnabled: false,
				scissorTestEnabled: false);
			pipelineDescription.PrimitiveTopology = PrimitiveTopology.TriangleStrip;
			pipelineDescription.ResourceLayouts = new ResourceLayout[] { resourceLayout };
			pipelineDescription.ShaderSet = new ShaderSetDescription(
				vertexLayouts: new VertexLayoutDescription[] { vertexLayout },
				shaders: Scene.Shaders);
			pipelineDescription.Outputs = _gd.SwapchainFramebuffer.OutputDescription;

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
		public void SubmitQuadDraw(Vector2 position, Vector2 size, RgbaFloat color)
		{
			SubmitQuadDraw(new Vector3(position, 0f), size, color);
		}

		public void SubmitQuadDraw(Vector3 position, Vector2 size, RgbaFloat color)
		{
			Renderable2D renderable = new Renderable2D();

			renderable.ColorBuffer = _gd.ResourceFactory.CreateBuffer(new BufferDescription(16, BufferUsage.UniformBuffer));
			_gd.UpdateBuffer(renderable.ColorBuffer, 0, color);

			renderable.TransformBuffer = _gd.ResourceFactory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));
			Matrix4x4 translation = Matrix4x4.CreateTranslation(position) * Matrix4x4.CreateScale(new Vector3(size, 1f));
			_gd.UpdateBuffer(renderable.TransformBuffer, 0, translation);

			ResourceFactory factory = _gd.ResourceFactory;
			ResourceLayout resourceLayout = factory.CreateResourceLayout(
				new ResourceLayoutDescription(
					new ResourceLayoutElementDescription("ViewProjection", ResourceKind.UniformBuffer, ShaderStages.Vertex),
					new ResourceLayoutElementDescription("Transform", ResourceKind.UniformBuffer, ShaderStages.Vertex),
					new ResourceLayoutElementDescription("Color", ResourceKind.UniformBuffer, ShaderStages.Fragment)));
			_gd.DisposeWhenIdle(resourceLayout);

			renderable.ResourceSet = factory.CreateResourceSet(new ResourceSetDescription(
				resourceLayout,
				Scene.CameraBuffer,
				renderable.TransformBuffer,
				renderable.ColorBuffer));

			Scene.RenderableList.Add(renderable);
		}

		protected override void BeginRender(CommandList cl)
		{
			cl.Begin();
			cl.SetFramebuffer(_gd.SwapchainFramebuffer);
			cl.SetViewport(0, new Viewport(0, 0, _gd.SwapchainFramebuffer.Width, _gd.SwapchainFramebuffer.Height, 0, 1));
			cl.SetFullViewports();
			cl.ClearColorTarget(0, RgbaFloat.Black);
			cl.SetVertexBuffer(0, Scene.VertexBuffer);
			cl.SetIndexBuffer(Scene.IndexBuffer, IndexFormat.UInt16);
			cl.SetPipeline(Scene.Pipeline);
		}

		public void Render()
		{
			Render(Scene);
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
		#endregion
	}
}
