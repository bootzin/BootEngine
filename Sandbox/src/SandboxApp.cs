using BootEngine;
using BootEngine.AssetManager;
using BootEngine.Events;
using BootEngine.Layers;
using BootEngine.Renderer.Cameras;
using ImGuiNET;
using Platforms.Windows;
using System;
using System.Numerics;
using Veldrid;

namespace Sandbox
{
	internal sealed class ExampleLayer : LayerBase
	{
		public ExampleLayer() : base("Example")
		{
			_graphicsDevice = Application<WindowsWindow>.App.Window.GraphicsDevice;
			float aspectRatio = (float)Application<WindowsWindow>.App.Window.SdlWindow.Width / Application<WindowsWindow>.App.Window.SdlWindow.Height;
			_cameraController = new OrthoCameraController(aspectRatio, _graphicsDevice.IsDepthRangeZeroToOne, _graphicsDevice.IsClipSpaceYInverted, true);
		}

		private readonly GraphicsDevice _graphicsDevice;
		private readonly OrthoCameraController _cameraController;
		private CommandList _commandList;
		private DeviceBuffer _vertexBuffer;
		private DeviceBuffer _indexBuffer;
		private DeviceBuffer _cameraBuffer;
		private DeviceBuffer _squareTransform;
		private Vector3 _squareColor;
		private DeviceBuffer _colorBuffer;
		private Pipeline _pipeline;
		private Pipeline _texPipeline;
		private ResourceSet _texResourceSet;
		private ResourceSet _resourceSet;
		private Texture _texture;
		private AssetManager assetManager;

		private AssetManager AssetManager
		{
			get
			{
				return assetManager ?? (assetManager = new AssetManager(_graphicsDevice));
			}
		}

		public override void OnAttach()
		{
			CreateResources();
		}

		public override void OnDetach()
		{
			_commandList.Dispose();
			_vertexBuffer.Dispose();
			_indexBuffer.Dispose();
			_cameraBuffer.Dispose();
			_colorBuffer.Dispose();
			_squareTransform.Dispose();
			_pipeline.Dispose();
			_resourceSet.Dispose();
			_texResourceSet.Dispose();
			_texture.Dispose();
			_texPipeline.Dispose();
		}

		public override void OnUpdate(float deltaSeconds)
		{
			_cameraController.Update(deltaSeconds);
			Draw();
		}

		public override void OnEvent(EventBase e)
		{
			_cameraController.OnEvent(e);
		}

		public override void OnGuiRender()
		{
			ImGui.Begin("Settings");
			ImGui.ColorEdit3("Square Color", ref _squareColor);
			ImGui.End();
		}

		private void CreateResources()
		{
			ResourceFactory factory = _graphicsDevice.ResourceFactory;

			_squareColor = new Vector3(.8f, .2f, .3f);
			_texture = AssetManager.LoadTexture2D("assets/textures/sampleFly.png", TextureUsage.Sampled);

			Span<VertexPositionTexture> quadVertices = stackalloc VertexPositionTexture[]
			{
				new VertexPositionTexture(new Vector2(-.5f, .5f), new Vector2(0.0f, 1.0f)),
				new VertexPositionTexture(new Vector2(.5f, .5f), new Vector2(1.0f, 1.0f)),
				new VertexPositionTexture(new Vector2(-.5f, -.5f), new Vector2(0.0f, 0.0f)),
				new VertexPositionTexture(new Vector2(.5f, -.5f), new Vector2(1.0f, 0.0f))
			};

			BufferDescription vbDescription = new BufferDescription(
				VertexPositionTexture.SizeInBytes * 4,
				BufferUsage.VertexBuffer);
			_vertexBuffer = factory.CreateBuffer(vbDescription);
			_graphicsDevice.UpdateBuffer(_vertexBuffer, 0, quadVertices.ToArray());

			Span<ushort> quadIndices = stackalloc ushort[] { 0, 1, 2, 3 };
			BufferDescription ibDescription = new BufferDescription(
				4 * sizeof(ushort),
				BufferUsage.IndexBuffer);
			_indexBuffer = factory.CreateBuffer(ibDescription);
			_graphicsDevice.UpdateBuffer(_indexBuffer, 0, quadIndices.ToArray());

			_cameraBuffer = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));
			_squareTransform = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer | BufferUsage.Dynamic));
			_colorBuffer = factory.CreateBuffer(new BufferDescription(16, BufferUsage.UniformBuffer | BufferUsage.Dynamic));

			VertexLayoutDescription vertexLayout = new VertexLayoutDescription(
				new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
				new VertexElementDescription("TexCoord", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2));

			Shader[] _shaders = AssetManager.GenerateShaders("FlatColor.glsl");
			Shader[] texShaders = AssetManager.GenerateShaders("Texture.glsl");

			ResourceLayout resourceLayout = factory.CreateResourceLayout(
				new ResourceLayoutDescription(
					new ResourceLayoutElementDescription("ViewProjection", ResourceKind.UniformBuffer, ShaderStages.Vertex),
					new ResourceLayoutElementDescription("Transform", ResourceKind.UniformBuffer, ShaderStages.Vertex),
					new ResourceLayoutElementDescription("Color", ResourceKind.UniformBuffer, ShaderStages.Fragment)));

			ResourceLayout texResourceLayout = factory.CreateResourceLayout(
				new ResourceLayoutDescription(
					new ResourceLayoutElementDescription("ViewProjection", ResourceKind.UniformBuffer, ShaderStages.Vertex),
					new ResourceLayoutElementDescription("Transform", ResourceKind.UniformBuffer, ShaderStages.Vertex),
					new ResourceLayoutElementDescription("Texture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
					new ResourceLayoutElementDescription("Sampler", ResourceKind.Sampler, ShaderStages.Fragment)));

			_graphicsDevice.DisposeWhenIdle(resourceLayout);
			_graphicsDevice.DisposeWhenIdle(texResourceLayout);
			_graphicsDevice.DisposeWhenIdle(_shaders[0]);
			_graphicsDevice.DisposeWhenIdle(_shaders[1]);
			_graphicsDevice.DisposeWhenIdle(texShaders[0]);
			_graphicsDevice.DisposeWhenIdle(texShaders[1]);

			// Create pipeline
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
				shaders: _shaders);
			pipelineDescription.Outputs = _graphicsDevice.SwapchainFramebuffer.OutputDescription;

			_resourceSet = factory.CreateResourceSet(new ResourceSetDescription(
				resourceLayout,
				_cameraBuffer,
				_squareTransform,
				_colorBuffer));

			_texResourceSet = factory.CreateResourceSet(new ResourceSetDescription(
				texResourceLayout,
				_cameraBuffer,
				_squareTransform,
				_texture,
				_graphicsDevice.LinearSampler));

			GraphicsPipelineDescription texPipelineDesc = pipelineDescription;
			texPipelineDesc.ShaderSet = new ShaderSetDescription(vertexLayouts: new VertexLayoutDescription[] { vertexLayout },
				shaders: texShaders);
			texPipelineDesc.ResourceLayouts = new ResourceLayout[] { texResourceLayout };
			texPipelineDesc.BlendState = BlendStateDescription.SingleAlphaBlend;

			_texPipeline = factory.CreateGraphicsPipeline(texPipelineDesc);

			_pipeline = factory.CreateGraphicsPipeline(pipelineDescription);

			_commandList = factory.CreateCommandList();
		}

		private void Draw()
		{
			_commandList.Begin();

			_commandList.SetFramebuffer(_graphicsDevice.SwapchainFramebuffer);
			_commandList.SetViewport(0, new Viewport(0, 0, _graphicsDevice.SwapchainFramebuffer.Width, _graphicsDevice.SwapchainFramebuffer.Height, 0, 1));
			_commandList.SetFullViewports();
			_commandList.ClearColorTarget(0, RgbaFloat.CornflowerBlue);

			_commandList.UpdateBuffer(_cameraBuffer, 0, _cameraController.Camera.ViewProjectionMatrix);

			_commandList.SetVertexBuffer(0, _vertexBuffer);
			_commandList.SetIndexBuffer(_indexBuffer, IndexFormat.UInt16);
			_commandList.SetPipeline(_pipeline);
			_commandList.SetGraphicsResourceSet(0, _resourceSet);

			for (int y = 0; y < 20; y++)
			{
				for (int x = 0; x < 20; x++)
				{
					Vector3 pos = new Vector3(x * 1.11f, y * 1.11f, 0f);
					Matrix4x4 translation = Matrix4x4.CreateTranslation(pos) * Matrix4x4.CreateScale(.1f);
					_commandList.UpdateBuffer(_squareTransform, 0, translation);
					_commandList.UpdateBuffer(_colorBuffer, 0, (pos / 10) - (_cameraController.Camera.Position * 1.1f) + _squareColor);

					_commandList.DrawIndexed(
						indexCount: 4,
						instanceCount: 1,
						indexStart: 0,
						vertexOffset: 0,
						instanceStart: 0);
				}
			}

			_commandList.SetPipeline(_texPipeline);
			_commandList.SetGraphicsResourceSet(0, _texResourceSet);
			_commandList.UpdateBuffer(_squareTransform, 0, Matrix4x4.CreateScale(1.5f));
			_commandList.DrawIndexed(
						indexCount: 4,
						instanceCount: 1,
						indexStart: 0,
						vertexOffset: 0,
						instanceStart: 0);

			_commandList.End();
			_graphicsDevice.SubmitCommands(_commandList);
		}

		public readonly struct VertexPositionTexture
		{
			public const uint SizeInBytes = 16;
			public readonly Vector2 Position;
			public readonly Vector2 TexCoord;

			public VertexPositionTexture(Vector2 position, Vector2 texCoord)
			{
				this.Position = position;
				this.TexCoord = texCoord;
			}
		}
	}

	public sealed class SandboxApp : Application<WindowsWindow>
	{
		public SandboxApp(GraphicsBackend backend) : base(backend)
		{
		}

		public static void Main()
		{
			var app = new SandboxApp(GraphicsBackend.Direct3D11);
			app.LayerStack.PushLayer(new ExampleLayer());
			app.Run();
			app.Dispose();
		}
	}
}
