using BootEngine.AssetsManager;
using BootEngine.Events;
using BootEngine.Layers.GUI;
using BootEngine.Renderer.Cameras;
using BootEngine.Utils.ProfilingTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Veldrid;

namespace BootEngine.Renderer
{
	public sealed class Renderer2D : Renderer<Renderer2D>, IDisposable
	{
		#region Constants
		private const int MAX_QUADS = 8;
		#endregion

		#region Properties
		public int InstanceCount { get; private set; }

		private readonly static GraphicsDevice _gd = Application.App.Window.GraphicsDevice;

		private readonly static CommandList _commandList = _gd.ResourceFactory.CreateCommandList();
		private List<InstanceVertexInfo> instanceList = new List<InstanceVertexInfo>(MAX_QUADS);
		private bool shouldFlush;
		private bool shouldClearBuffers;

		#region RendererData
		public static Texture WhiteTexture { get; internal set; }
		internal DeviceBuffer IndexBuffer { get; set; }
		internal DeviceBuffer VertexBuffer { get; set; }
		internal DeviceBuffer InstancesVertexBuffer { get; set; }
		internal DeviceBuffer CameraBuffer { get; set; }
		internal ResourceLayout ResourceLayout { get; set; }
		public GraphicsPipelineDescription PipelineDescrition { get; private set; }
		internal Pipeline ActivePipeline { get; private set; }
		internal Framebuffer ActiveFramebuffer { get; private set; }
		public RenderStats Stats { get; } = new RenderStats();

		private Dictionary<Texture, InstancingTextureData> DataPerTexture { get; } = new Dictionary<Texture, InstancingTextureData>();
		#endregion
		#endregion

		#region Constructor
		public Renderer2D()
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(typeof(Renderer2D));
#endif
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

			IndexBuffer = factory.CreateBuffer(ibDescription);
			VertexBuffer = factory.CreateBuffer(vbDescription);
			_gd.UpdateBuffer(IndexBuffer, 0, quadIndices.ToArray());
			_gd.UpdateBuffer(VertexBuffer, 0, quadVertices.ToArray());

			WhiteTexture = factory.CreateTexture(TextureDescription.Texture2D(
				1u, // Width
				1u, // Height
				1,  // Miplevel
				1,  // ArrayLayers
				PixelFormat.R8_G8_B8_A8_UNorm,
				TextureUsage.Sampled));
			uint[] whiteTexture = { 0xffffffff };
			_gd.UpdateTexture(
				WhiteTexture,
				whiteTexture,
				0,  // x
				0,  // y
				0,  // z
				1u, // Width
				1u, // Height
				1,  // Depth
				0,  // Miplevel
				0); // ArrayLayer
			WhiteTexture.Name = "WhiteTex";

			InstancesVertexBuffer = factory.CreateBuffer(new BufferDescription(InstanceVertexInfo.SizeInBytes * MAX_QUADS, BufferUsage.VertexBuffer));
			CameraBuffer = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));

			VertexLayoutDescription sharedVertexLayout = new VertexLayoutDescription(
				new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
				new VertexElementDescription("TexCoord", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2));

			// TODO: Try passing the transform matrix as 4 Vec4 or as a structured buffer
			VertexLayoutDescription instanceVertexLayout = new VertexLayoutDescription(
				new VertexElementDescription("InstancePosition", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
				new VertexElementDescription("InstanceScale", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
				new VertexElementDescription("InstanceRotation", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
				new VertexElementDescription("InstanceColor", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4));
			instanceVertexLayout.InstanceStepRate = 1;

			ResourceLayout = factory.CreateResourceLayout(
				new ResourceLayoutDescription(
					new ResourceLayoutElementDescription("ViewProjection", ResourceKind.UniformBuffer, ShaderStages.Vertex),
					new ResourceLayoutElementDescription("Texture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
					new ResourceLayoutElementDescription("Sampler", ResourceKind.Sampler, ShaderStages.Fragment)));

			GraphicsPipelineDescription pipelineDescription = new GraphicsPipelineDescription();
			pipelineDescription.BlendState = BlendStateDescription.SingleAlphaBlend;
			pipelineDescription.DepthStencilState = DepthStencilStateDescription.DepthOnlyLessEqual;
			pipelineDescription.RasterizerState = RasterizerStateDescription.CullNone;
			pipelineDescription.PrimitiveTopology = PrimitiveTopology.TriangleStrip;
			pipelineDescription.ResourceLayouts = new ResourceLayout[] { ResourceLayout };
			pipelineDescription.ShaderSet = new ShaderSetDescription(
				vertexLayouts: new VertexLayoutDescription[] { sharedVertexLayout, instanceVertexLayout },
				shaders: AssetManager.GenerateShadersFromFile("TexturedInstancing.glsl"));
			pipelineDescription.Outputs = _gd.SwapchainFramebuffer.OutputDescription;

			SetPipelineDescrition(pipelineDescription, _gd.SwapchainFramebuffer);

			DataPerTexture.Add(WhiteTexture, new InstancingTextureData()
			{
				ResourceSet = factory.CreateResourceSet(new ResourceSetDescription(
					ResourceLayout,
					CameraBuffer,
					WhiteTexture,
					_gd.LinearSampler)),
				Count = 0
			});
		}

		public void ResetStats()
		{
			Stats.DrawCalls = 0;
		}
		#endregion
		public void BeginScene(Camera cam, Matrix4x4 transform, bool shouldClearBuffers = true)
		{
			this.shouldClearBuffers = shouldClearBuffers;

			Matrix4x4.Invert(transform, out Matrix4x4 view);

			_gd.UpdateBuffer(CameraBuffer, 0, view * cam.ProjectionMatrix);
		}

		public void EndScene()
		{
			//
		}

		#region Primitives
		public void QueueQuad(Matrix4x4 transform, Vector4 color)
		{
			throw new NotImplementedException();
		}

		// TODO: Optimize this function
		public void QueueQuad(Vector3 position, Vector3 scale, Vector3 rotation, Vector4 color, Texture tex = null)
		{
			var info = new InstanceVertexInfo(position, scale, rotation, color);
			if (tex != null)
			{
				if (!DataPerTexture.ContainsKey(tex))
				{
					DataPerTexture.Add(tex, new InstancingTextureData()
					{
						ResourceSet = _gd.ResourceFactory.CreateResourceSet(new ResourceSetDescription(
							ResourceLayout,
							CameraBuffer,
							tex,
							_gd.LinearSampler)),
						Count = 1,
						IndexStart = DataPerTexture.Values.Max(data => data.LastInstanceIndex)
					});
				}
				else
				{
					DataPerTexture[tex].Count++;
				}
				instanceList.Insert((int)DataPerTexture[tex].LastInstanceIndex - 1, info);
			}
			else
			{
				DataPerTexture[WhiteTexture].Count++;
				instanceList.Insert((int)DataPerTexture[WhiteTexture].LastInstanceIndex - 1, info);
			}

			InstanceCount++;
			shouldFlush = true;
		}
		#endregion

		public void Flush()
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			shouldFlush = false;

			if (InstancesVertexBuffer.SizeInBytes < instanceList.Count * InstanceVertexInfo.SizeInBytes)
			{
				InstanceCount = instanceList.Count;
				InstancesVertexBuffer = _gd.ResourceFactory.CreateBuffer(new BufferDescription((uint)(instanceList.Count * InstanceVertexInfo.SizeInBytes), BufferUsage.VertexBuffer));
			}

			_gd.UpdateBuffer(InstancesVertexBuffer, 0, instanceList.ToArray());
		}

		#region Renderer
		public void Render()
		{
			Render(_commandList);
		}
		protected override void BeginRender(CommandList cl)
		{
			if (shouldFlush)
				Flush();
			cl.Begin();
			cl.SetFramebuffer(ActiveFramebuffer);
			cl.SetFullViewport(0);
			if (shouldClearBuffers)
			{
				cl.ClearColorTarget(0, RgbaFloat.Grey);
				cl.ClearDepthStencil(1f);
			}
			cl.SetPipeline(ActivePipeline);
			cl.SetIndexBuffer(IndexBuffer, IndexFormat.UInt16);
			cl.SetVertexBuffer(0, VertexBuffer);
			cl.SetVertexBuffer(1, InstancesVertexBuffer);
		}

		protected override void BatchRender(CommandList cl)
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			uint instanceStart = 0;
			foreach (var entry in DataPerTexture)
			{
				uint instancePerTexCount = entry.Value.Count;
				if (instancePerTexCount > 0)
				{
					Stats.DrawCalls++;
					cl.SetGraphicsResourceSet(0, entry.Value.ResourceSet);
					cl.DrawIndexed(
						indexCount: 4,
						instanceCount: instancePerTexCount,
						indexStart: 0,
						vertexOffset: 0,
						instanceStart: instanceStart);
					instanceStart += instancePerTexCount;
				}
			}
		}

		protected override void EndRender(CommandList cl)
		{
			cl.End();
			_gd.SubmitCommands(cl);
			Stats.InstanceCount = InstanceCount;
			InstanceCount = 0;
			foreach (var entry in DataPerTexture)
			{
				if (entry.Key != WhiteTexture)
					DataPerTexture.Remove(entry.Key);
				else
					entry.Value.Count = 0u;
			}
			instanceList = new List<InstanceVertexInfo>(MAX_QUADS);
		}
		#endregion

		public void SetPipelineDescrition(GraphicsPipelineDescription pd, Framebuffer targetFb, bool clearMainFramebuffer = false)
		{
			ActiveFramebuffer = targetFb;
			PipelineDescrition = pd;
			ActivePipeline = _gd.ResourceFactory.CreateGraphicsPipeline(pd);
			ImGuiLayer.ShouldClearBuffers = clearMainFramebuffer; // TODO: This shouldn't be here, but I'm unsure on how to do it properly
		}

		internal void ResizeSwapchain(WindowResizeEvent we)
		{
			// Necessary logic to deall with DirectX framebuffer recreation
			if (ActiveFramebuffer == _gd.SwapchainFramebuffer)
			{
				Application.App.Window.GraphicsDevice.MainSwapchain.Resize((uint)we.Width, (uint)we.Height);
				if (ActiveFramebuffer != _gd.SwapchainFramebuffer)
					ActiveFramebuffer = _gd.SwapchainFramebuffer;
			}
			else
			{
				Application.App.Window.GraphicsDevice.MainSwapchain.Resize((uint)we.Width, (uint)we.Height);
			}
		}

		public void Dispose()
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			IndexBuffer.Dispose();
			VertexBuffer.Dispose();
			InstancesVertexBuffer.Dispose();
			CameraBuffer.Dispose();
			if (!ActivePipeline.IsDisposed)
				ActivePipeline.Dispose();
			ResourceLayout.Dispose();
			foreach (var kv in DataPerTexture)
			{
				kv.Value.ResourceSet.Dispose();
				kv.Key.Dispose();
			}
		}

		private readonly struct VertexPositionTexture
		{
			public const uint SizeInBytes = 20;
			public readonly Vector3 Position { get; }
			public readonly Vector2 TexCoord { get; }

			public VertexPositionTexture(Vector3 position, Vector2 texCoord)
			{
				Position = position;
				TexCoord = texCoord;
			}
		}

		private readonly struct InstanceVertexInfo
		{
			public const uint SizeInBytes = 52;

			public InstanceVertexInfo(Vector3 position, Vector3 scale, Vector3 rotation, Vector4 color)
			{
				Position = position;
				Scale = scale;
				Rotation = rotation;
				Color = color;
			}

			public readonly Vector3 Position { get; }
			public readonly Vector3 Scale { get; }
			public readonly Vector3 Rotation { get; }
			public readonly Vector4 Color { get; }
		}

		private class InstancingTextureData
		{
			public ResourceSet ResourceSet { get; set; }
			public uint Count { get; set; }
			public uint IndexStart { get; set; }
			public uint LastInstanceIndex => IndexStart + Count;
		}
	}

	#region Aux
	public class RenderStats
	{
		public int InstanceCount { get; internal set; }
		public int DrawCalls { get; internal set; }
	}

	public ref struct Renderable2DParameters
	{
		public string Name { get; set; }
		public Vector4 Color { get; set; }
		public Vector3 Position { get; set; }
		public Vector3 Size { get; set; }
		public Vector3 Rotation { get; set; }
		public Texture Texture { get; set; }
	}
	#endregion
}
