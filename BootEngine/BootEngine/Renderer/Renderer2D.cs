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
		private const int MAX_QUADS = 1;
		#endregion

		#region Properties
		public int InstanceCount { get; private set; }

		private readonly static GraphicsDevice _gd = Application.App.Window.GraphicsDevice;

		private readonly static CommandList _commandList = _gd.ResourceFactory.CreateCommandList();
		private readonly Dictionary<string, ShaderData> ShaderData = new Dictionary<string, ShaderData>();
		private Camera activeCamera;
		private bool shouldClearBuffers = false;

		#region RendererData
		public static Texture WhiteTexture { get; private set; }
		internal DeviceBuffer CameraBuffer { get; set; }
		internal Framebuffer ActiveFramebuffer { get; private set; }
		public RenderStats Stats { get; } = new RenderStats();

		private Dictionary<int, InstancingTextureData> DataPerObject { get; } = new Dictionary<int, InstancingTextureData>();
		private readonly Dictionary<int, Pipeline> PipelineList = new Dictionary<int, Pipeline>();
		private readonly VertexLayoutDescription[] vertexLayoutDescriptions;
		#endregion
		#endregion

		#region Constructor
		public Renderer2D()
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(typeof(Renderer2D));
#endif
			ResourceFactory factory = _gd.ResourceFactory;

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

			vertexLayoutDescriptions = new VertexLayoutDescription[] { sharedVertexLayout, instanceVertexLayout };

			CameraBuffer = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));

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
		}

		public void AddShader(string shaderSetName, ShaderData shaderData) => ShaderData.Add(shaderSetName, shaderData);

		public void ResetStats() => Stats.DrawCalls = 0;
		#endregion

		public void BeginScene(Camera cam, Matrix4x4 transform, bool shouldClearBuffers = true)
		{
			activeCamera = cam;

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
		public void QueueQuad(Vector3 position, Vector3 scale, Vector3 rotation, Vector4 color, RenderData2D spriteData, Material material)
		{
			var info = new InstanceVertex2D(position, scale, rotation, color * material.Color);
			int hash = spriteData.GetHashCode() + material.ShaderSetName.GetHashCode();
			if (!DataPerObject.ContainsKey(hash))
			{
				DataPerObject.Add(hash, new InstancingTextureData()
				{
					ResourceSet = _gd.ResourceFactory.CreateResourceSet(new ResourceSetDescription(
						ShaderData[material.ShaderSetName].ResourceLayouts[0],
						CameraBuffer,
						spriteData.Texture ?? WhiteTexture,
						_gd.LinearSampler)),
					Render2DData = spriteData,
					InstancesVertex2DBuffer = _gd.ResourceFactory.CreateBuffer(new BufferDescription(InstanceVertex2D.SizeInBytes * MAX_QUADS, BufferUsage.VertexBuffer)),
					ShaderSetName = material.ShaderSetName,
					Count = 1,
				});
			}
			else
			{
				DataPerObject[hash].Count++;
			}

			InstanceCount++;
			DataPerObject[hash].InstanceList.Insert((int)DataPerObject[hash].Count - 1, info);
			DataPerObject[hash].ShouldFlush = true;
		}
		#endregion

		private void Flush(InstancingTextureData data)
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			data.ShouldFlush = false;

			if (data.InstancesVertex2DBuffer.SizeInBytes < data.InstanceList.Count * InstanceVertex2D.SizeInBytes)
			{
				InstanceCount = data.InstanceList.Count;
				data.InstancesVertex2DBuffer = _gd.ResourceFactory.CreateBuffer(new BufferDescription((uint)(data.InstanceList.Count * InstanceVertex2D.SizeInBytes), BufferUsage.VertexBuffer));
			}

			_gd.UpdateBuffer(data.InstancesVertex2DBuffer, 0, data.InstanceList.ToArray());
		}

		#region Renderer
		private void CheckPipelines(string shaderName, ShaderData shaderData)
		{
			var pd = new GraphicsPipelineDescription();
			pd.BlendState = activeCamera.BlendState;
			pd.RasterizerState = activeCamera.RasterizerState;
			pd.DepthStencilState = activeCamera.DepthStencilState;
			pd.ResourceBindingModel = ResourceBindingModel.Improved;
			pd.PrimitiveTopology = PrimitiveTopology.TriangleStrip; // TODO: Change to TriangleList
			pd.ResourceLayouts = shaderData.ResourceLayouts;
			pd.ShaderSet = new ShaderSetDescription(vertexLayoutDescriptions, shaderData.Shaders);
			pd.Outputs = activeCamera.RenderTarget.OutputDescription;

			if (PipelineList.TryAdd(pd.GetHashCode(), _gd.ResourceFactory.CreateGraphicsPipeline(ref pd)))
			{
				PipelineList[pd.GetHashCode()].Name = shaderName;
			}
		}

		public void Render()
		{
			foreach (var shaderData in ShaderData)
			{
				CheckPipelines(shaderData.Key, shaderData.Value);
			}

			Render(_commandList, PipelineList);
		}

		protected override void BeginRender(CommandList cl)
		{
			cl.Begin();
			cl.SetFramebuffer(activeCamera.RenderTarget);
			cl.SetFullViewport(0);
			if (shouldClearBuffers)
			{
				cl.ClearColorTarget(0, Utils.ColorF.DarkGrey);
				cl.ClearDepthStencil(1f);
			}
		}

		protected override void InnerRender(CommandList cl, Pipeline pipeline)
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			string pipelineName = pipeline.Name;
			var renderables = DataPerObject.Values.Where(data => data.ShaderSetName == pipelineName);
			if (!renderables.Any())
				return;

			cl.SetPipeline(pipeline);
			foreach (InstancingTextureData data in renderables)
			{
				cl.SetIndexBuffer(data.Render2DData.IndexBuffer, IndexFormat.UInt16);
				cl.SetVertexBuffer(0, data.Render2DData.VertexBuffer);
				if (data.ShouldFlush)
					Flush(data);
				cl.SetVertexBuffer(1, data.InstancesVertex2DBuffer);

				uint instancePerTexCount = data.Count;
				if (instancePerTexCount > 0)
				{
					Stats.DrawCalls++;
					cl.SetGraphicsResourceSet(0, data.ResourceSet);
					cl.DrawIndexed(
						indexCount: (uint)data.Render2DData.Indices.Length,
						instanceCount: instancePerTexCount,
						indexStart: 0,
						vertexOffset: 0,
						instanceStart: 0);
				}
			}
		}

		protected override void EndRender(CommandList cl)
		{
			cl.End();
			_gd.SubmitCommands(cl);
			Stats.InstanceCount = InstanceCount;
			InstanceCount = 0;
			foreach (var entry in DataPerObject)
			{
				entry.Value.Count = 0u;
				entry.Value.InstanceList = new List<InstanceVertex2D>(entry.Value.InstanceList.Count);
			}
			//DataPerObject.Clear();
		}
		#endregion

		internal void ResizeSwapchain(WindowResizeEvent we)
		{
			// Necessary logic to deal with DirectX framebuffer recreation
			if (activeCamera.RenderTarget == _gd.SwapchainFramebuffer)
			{
				Application.App.Window.GraphicsDevice.MainSwapchain.Resize((uint)we.Width, (uint)we.Height);
				activeCamera.RenderTarget = _gd.SwapchainFramebuffer;
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
			CameraBuffer.Dispose();
			foreach (var data in DataPerObject)
			{
				data.Value.ResourceSet.Dispose();
				data.Value.InstancesVertex2DBuffer.Dispose();
				data.Value.InstanceList = null;
				data.Value.Render2DData.Dispose();
			}

			foreach (var pipeline in PipelineList.Values)
			{
				pipeline.Dispose();
			}

			foreach (var data in ShaderData.Values)
			{
				foreach (var shader in data.Shaders)
				{
					shader.Dispose();
				}

				foreach (var resource in data.ResourceLayouts)
				{
					resource.Dispose();
				}
			}
		}

		private readonly struct InstanceVertex2D
		{
			public const uint SizeInBytes = 52;

			public InstanceVertex2D(Vector3 position, Vector3 scale, Vector3 rotation, Vector4 color)
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
			public DeviceBuffer InstancesVertex2DBuffer { get; set; }
			public List<InstanceVertex2D> InstanceList { get; set; } = new List<InstanceVertex2D>(MAX_QUADS);
			public RenderData2D Render2DData { get; set; }
			public string ShaderSetName { get; set; }
			public bool ShouldFlush { get; set; }
			public uint Count { get; set; }
		}
	}

	#region Stats
	public class RenderStats
	{
		public int InstanceCount { get; internal set; }
		public int DrawCalls { get; internal set; }
	}
	#endregion
}
