using BootEngine;
using BootEngine.Events;
using BootEngine.Layers;
using BootEngine.Renderer;
using Platforms.Windows;
using System;
using System.Numerics;
using System.Text;
using Utils;
using Veldrid;
using Veldrid.SPIRV;

namespace Sandbox
{
	internal class ExampleLayer : LayerBase
	{
		#region Shaders
		private const string VertexCode = @"
			#version 430 core

			uniform ViewProjection
			{
				mat4 view_projection_matrix;
			};

			layout(location = 0) in vec2 Position;
			layout(location = 1) in vec4 Color;

			layout(location = 0) out vec4 fsin_Color;

			void main()
			{
				gl_Position = view_projection_matrix * vec4(Position, 0, 1);
				fsin_Color = Color;
			}";

		private const string FragmentCode = @"
			#version 430 core

			layout(location = 0) in vec4 fsin_Color;
			layout(location = 0) out vec4 fsout_Color;

			void main()
			{
				fsout_Color = fsin_Color;
			}";
		#endregion

		public ExampleLayer() : base("Example")
		{
			_graphicsDevice = Application<WindowsWindow>.App.Window.GraphicsDevice;
		}

		private readonly GraphicsDevice _graphicsDevice;
		private CommandList _commandList;
		private DeviceBuffer _vertexBuffer;
		private DeviceBuffer _indexBuffer;
		private DeviceBuffer _cameraBuffer;
		private Pipeline _pipeline;
		private ResourceSet _resourceSet;
		private OrthoCamera _camera;

		public override void OnAttach()
		{
			CreateResources();
		}

		public override void OnUpdate()
		{
			_camera.Update();
			Draw();
		}

		public override void OnEvent(EventBase @event)
		{
			//
		}

		public override void OnGuiRender()
		{
			//
		}

		private void CreateResources()
		{
			ResourceFactory factory = _graphicsDevice.ResourceFactory;

			_camera = new OrthoCamera(-1.0f, 1.0f, -1f, 1f, _graphicsDevice.IsDepthRangeZeroToOne, _graphicsDevice.IsClipSpaceYInverted);

			Span<VertexPositionColor> quadVertices = stackalloc VertexPositionColor[]
			{
				new VertexPositionColor(new Vector2(-.75f, .75f), RgbaFloat.Red),
				new VertexPositionColor(new Vector2(.75f, .75f), RgbaFloat.Green),
				new VertexPositionColor(new Vector2(-.75f, -.75f), RgbaFloat.Blue),
				new VertexPositionColor(new Vector2(.75f, -.75f), RgbaFloat.Yellow)
			};
			BufferDescription vbDescription = new BufferDescription(
				4 * VertexPositionColor.SizeInBytes,
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
			_graphicsDevice.UpdateBuffer(_cameraBuffer, 0, _camera.ViewProjectionMatrix);

			VertexLayoutDescription vertexLayout = new VertexLayoutDescription(
				new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
				new VertexElementDescription("Color", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4));

			ShaderDescription vertexShaderDesc = new ShaderDescription(
				ShaderStages.Vertex,
				Encoding.UTF8.GetBytes(VertexCode),
				"main");
			ShaderDescription fragmentShaderDesc = new ShaderDescription(
				ShaderStages.Fragment,
				Encoding.UTF8.GetBytes(FragmentCode),
				"main");

			Shader[] _shaders = factory.CreateFromSpirv(vertexShaderDesc, fragmentShaderDesc);

			var resourceLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(new ResourceLayoutElementDescription("ViewProjection", ResourceKind.UniformBuffer, ShaderStages.Vertex)));

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
				depthClipEnabled: true,
				scissorTestEnabled: false);
			pipelineDescription.PrimitiveTopology = PrimitiveTopology.TriangleStrip;
			pipelineDescription.ResourceLayouts = new[] { resourceLayout };
			pipelineDescription.ShaderSet = new ShaderSetDescription(
				vertexLayouts: new VertexLayoutDescription[] { vertexLayout },
				shaders: _shaders);
			pipelineDescription.Outputs = _graphicsDevice.SwapchainFramebuffer.OutputDescription;

			_resourceSet = factory.CreateResourceSet(new ResourceSetDescription(resourceLayout, _cameraBuffer));

			_pipeline = factory.CreateGraphicsPipeline(pipelineDescription);

			_commandList = factory.CreateCommandList();
		}

		private void Draw()
		{
			_commandList.Begin();

			// We want to render directly to the output window.
			_commandList.SetFramebuffer(_graphicsDevice.SwapchainFramebuffer);
			_commandList.SetViewport(0, new Viewport(0, 0, _graphicsDevice.SwapchainFramebuffer.Width, _graphicsDevice.SwapchainFramebuffer.Height, 0, 1));
			_commandList.SetFullViewports();
			_commandList.ClearColorTarget(0, RgbaFloat.CornflowerBlue);

			_graphicsDevice.UpdateBuffer(_cameraBuffer, 0, _camera.ViewProjectionMatrix);

			// Set all relevant state to draw our quad.
			_commandList.SetVertexBuffer(0, _vertexBuffer);
			_commandList.SetIndexBuffer(_indexBuffer, IndexFormat.UInt16);
			_commandList.SetPipeline(_pipeline);
			_commandList.SetGraphicsResourceSet(0, _resourceSet);
			// Issue a Draw command for a single instance with 4 indices.
			_commandList.DrawIndexed(
				indexCount: 4,
				instanceCount: 1,
				indexStart: 0,
				vertexOffset: 0,
				instanceStart: 0);

			_commandList.End();
			_graphicsDevice.SubmitCommands(_commandList);
		}

		private readonly struct VertexPositionColor
		{
			public const uint SizeInBytes = 24;
			public readonly Vector2 Position;
			public readonly RgbaFloat Color;

			public VertexPositionColor(Vector2 position, RgbaFloat color)
			{
				this.Position = position;
				this.Color = color;
			}
		}
	}

	public class SandboxApp : Application<WindowsWindow>
	{
		public SandboxApp(GraphicsBackend backend) : base(backend)
		{
		}

		public static void Main()
		{
			var app = new SandboxApp(GraphicsBackend.Vulkan);
			app.LayerStack.PushLayer(new ExampleLayer());
			app.Run();
			app.Dispose();
		}
	}
}
