using BootEngine;
using BootEngine.Events;
using BootEngine.Layers;
using BootEngine.Window;
using Platforms.Windows;
using System;
using System.Numerics;
using System.Text;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.SPIRV;

namespace Sandbox
{
	internal class TestLayer : LayerBase
	{
		public TestLayer() : base("TestLayer") { }

		public override void OnUpdate()
		{
			//
		}

		public override void OnEvent(EventBase @event)
		{
			//
		}
	}

	public class SandboxApp : Application<WindowsWindow>
	{
		private const string VertexCode = @"
#version 430

layout(location = 0) in vec2 Position;
layout(location = 1) in vec4 Color;

layout(location = 0) out vec4 fsin_Color;

void main()
{
    gl_Position = vec4(Position, 0, 1);
    fsin_Color = Color;
}";

		private const string FragmentCode = @"
#version 430

layout(location = 0) in vec4 fsin_Color;
layout(location = 0) out vec4 fsout_Color;

void main()
{
    fsout_Color = fsin_Color;
}";

		private static GraphicsDevice _graphicsDevice;
		private static CommandList _commandList;
		private static DeviceBuffer _vertexBuffer;
		private static DeviceBuffer _indexBuffer;
		private static Pipeline _pipeline;

		public static void Main()
		{
			var app = new SandboxApp();
			app.LayerStack.PushLayer(new TestLayer());
			app.Run();
			app.Dispose();

			BootEngine.Log.Logger.Init();
			WindowStartup.CreateWindowAndGraphicsDevice(new WindowProps(), new GraphicsDeviceOptions() { PreferStandardClipSpaceYDirection = true, PreferDepthRangeZeroToOne = true }, GraphicsBackend.Vulkan, out Sdl2Window window, out _graphicsDevice);

			CreateResources();

			while (window.Exists)
			{
				window.PumpEvents();

				if (window.Exists)
				{
					Draw();
				}
			}
		}

		private static void CreateResources()
		{
			ResourceFactory factory = _graphicsDevice.ResourceFactory;

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

			Span<ushort> quadIndices = stackalloc ushort[]{ 0, 1, 2, 3 };
			BufferDescription ibDescription = new BufferDescription(
				4 * sizeof(ushort),
				BufferUsage.IndexBuffer);
			_indexBuffer = factory.CreateBuffer(ibDescription);
			_graphicsDevice.UpdateBuffer(_indexBuffer, 0, quadIndices.ToArray());

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

			// Create pipeline
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
			pipelineDescription.ResourceLayouts = Array.Empty<ResourceLayout>();
			pipelineDescription.ShaderSet = new ShaderSetDescription(
				vertexLayouts: new VertexLayoutDescription[] { vertexLayout },
				shaders: _shaders);
			pipelineDescription.Outputs = _graphicsDevice.SwapchainFramebuffer.OutputDescription;

			_pipeline = factory.CreateGraphicsPipeline(pipelineDescription);

			_commandList = factory.CreateCommandList();
		}

		private static void Draw()
		{
			// Begin() must be called before commands can be issued.
			_commandList.Begin();

			// We want to render directly to the output window.
			_commandList.SetFramebuffer(_graphicsDevice.SwapchainFramebuffer);
			_commandList.ClearColorTarget(0, RgbaFloat.Black);

			// Set all relevant state to draw our quad.
			_commandList.SetVertexBuffer(0, _vertexBuffer);
			_commandList.SetIndexBuffer(_indexBuffer, IndexFormat.UInt16);
			_commandList.SetPipeline(_pipeline);
			// Issue a Draw command for a single instance with 4 indices.
			_commandList.DrawIndexed(
				indexCount: 4,
				instanceCount: 1,
				indexStart: 0,
				vertexOffset: 0,
				instanceStart: 0);

			// End() must be called before commands can be submitted for execution.
			_commandList.End();
			_graphicsDevice.SubmitCommands(_commandList);

			// Once commands have been submitted, the rendered image can be presented to the application window.
			_graphicsDevice.SwapBuffers();
		}

		private struct VertexPositionColor
		{
			public const uint SizeInBytes = 24;
			public Vector2 Position;
			public RgbaFloat Color;

			public VertexPositionColor(Vector2 position, RgbaFloat color)
			{
				Position = position;
				Color = color;
			}
		}
	}
}
