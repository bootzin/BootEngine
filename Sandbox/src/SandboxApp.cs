﻿using BootEngine;
using BootEngine.Events;
using BootEngine.Layers;
using BootEngine.Renderer;
using ImGuiNET;
using Platforms.Windows;
using System;
using System.Numerics;
using System.Text;
using Veldrid;
using Veldrid.SPIRV;

namespace Sandbox
{
	internal class ExampleLayer : LayerBase
	{
		#region Shaders
		private const string VertexCode = @"
			#version 430 core

			layout(set = 0, binding = 0) uniform ViewProjection
			{
				mat4 view_projection_matrix;
			};

			layout(set = 0, binding = 1) uniform Transform
			{
				mat4 model_matrix;
			};

			layout(location = 0) in vec2 Position;

			void main()
			{
				gl_Position = view_projection_matrix * model_matrix * vec4(Position, 0, 1);
			}";

		private const string FragmentCode = @"
			#version 430 core

			layout(set = 0, binding = 2) uniform Color
			{
				vec4 fsin_Color;
			};

			layout(location = 0) out vec4 fsout_Color;

			void main()
			{
				fsout_Color = fsin_Color;
			}";

		private const string TextureVertexCode = @"
			#version 430 core

			layout(set = 0, binding = 0) uniform ViewProjection
			{
				mat4 view_projection_matrix;
			};

			layout(set = 0, binding = 1) uniform Transform
			{
				mat4 model_matrix;
			};

			layout(location = 0) in vec2 Position;
			layout(location = 1) in vec2 TexCoord;

			layout(location = 0) out vec2 outTexCoord;

			void main()
			{
				outTexCoord = TexCoord;
				gl_Position = view_projection_matrix * model_matrix * vec4(Position, 0, 1);
			}";

		private const string TextureFragmentCode = @"
			#version 430 core

			layout(location = 0) in vec2 TexCoord;
			layout(location = 0) out vec4 color;

			layout(set = 0, binding = 2) uniform texture2D Texture;
			layout(set = 0, binding = 3) uniform sampler Sampler;

			void main()
			{
				color = texture(sampler2D(Texture, Sampler), TexCoord);
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
		private DeviceBuffer _squareTransform;
		private DeviceBuffer _colorBuffer;
		private Vector3 _squareColor;
		private Pipeline _pipeline;
		private Pipeline _texPipeline;
		private ResourceSet _texResourceSet;
		private ResourceSet _resourceSet;
		private Texture _texture;
		private OrthoCamera _camera;

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
			_camera.Update(deltaSeconds);
			Draw();
		}

		public override void OnEvent(EventBase @event)
		{
			//
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

			_commandList = factory.CreateCommandList();

			_camera = new OrthoCamera(-1.6f, 1.6f, -.9f, .9f, _graphicsDevice.IsDepthRangeZeroToOne, _graphicsDevice.IsClipSpaceYInverted);
			_squareColor = new Vector3(.8f, .2f, .3f);
			_texture = Utils.Util.LoadTexture2D(_graphicsDevice, "assets/textures/sampleFly.png", TextureUsage.Sampled);

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

			ShaderDescription vertexShaderDesc = new ShaderDescription(
				ShaderStages.Vertex,
				Encoding.UTF8.GetBytes(VertexCode),
				"main");
			ShaderDescription fragmentShaderDesc = new ShaderDescription(
				ShaderStages.Fragment,
				Encoding.UTF8.GetBytes(FragmentCode),
				"main");

			Shader[] _shaders = factory.CreateFromSpirv(vertexShaderDesc, fragmentShaderDesc);

			ShaderDescription texVertexShaderDesc = new ShaderDescription(
				ShaderStages.Vertex,
				Encoding.UTF8.GetBytes(TextureVertexCode),
				"main");
			ShaderDescription texFragmentShaderDesc = new ShaderDescription(
				ShaderStages.Fragment,
				Encoding.UTF8.GetBytes(TextureFragmentCode),
				"main");

			Shader[] texShaders = factory.CreateFromSpirv(texVertexShaderDesc, texFragmentShaderDesc);

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
			pipelineDescription.ResourceLayouts = new[] { resourceLayout };
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
			texPipelineDesc.ResourceLayouts = new[] { texResourceLayout };
			texPipelineDesc.BlendState = BlendStateDescription.SingleAlphaBlend;

			_texPipeline = factory.CreateGraphicsPipeline(texPipelineDesc);

			_pipeline = factory.CreateGraphicsPipeline(pipelineDescription);
		}

		private void Draw()
		{
			_commandList.Begin();

			_commandList.SetFramebuffer(_graphicsDevice.SwapchainFramebuffer);
			_commandList.SetViewport(0, new Viewport(0, 0, _graphicsDevice.SwapchainFramebuffer.Width, _graphicsDevice.SwapchainFramebuffer.Height, 0, 1));
			_commandList.SetFullViewports();
			_commandList.ClearColorTarget(0, RgbaFloat.CornflowerBlue);

			_commandList.UpdateBuffer(_cameraBuffer, 0, _camera.ViewProjectionMatrix);

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
					_commandList.UpdateBuffer(_colorBuffer, 0, (pos / 10) - (_camera.Position * 1.1f) + _squareColor);

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

		private readonly struct VertexPositionTexture
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

	public class SandboxApp : Application<WindowsWindow>
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
