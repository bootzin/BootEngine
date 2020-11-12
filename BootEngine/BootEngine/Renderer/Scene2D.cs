using BootEngine.Utils.ProfilingTools;
using System.Collections.Generic;
using Veldrid;

namespace BootEngine.Renderer
{
	public sealed class Scene2D : Scene
	{
		private bool renderToFramebuffer = false;

		internal DeviceBuffer IndexBuffer { get; set; }
		internal DeviceBuffer VertexBuffer { get; set; }
		internal DeviceBuffer InstancesVertexBuffer { get; set; }
		internal DeviceBuffer CameraBuffer { get; set; }
		internal Dictionary<Texture, InstancingTextureData> DataPerTexture { get; set; } = new Dictionary<Texture, InstancingTextureData>();
		public ResourceLayout ResourceLayout { get; set; }
		internal Pipeline ActivePipeline { get; set; }
		internal Pipeline MainPipeline { get; set; }
		internal Pipeline FramebufferPipeline { get; set; }
		public Shader[] Shaders { get; set; }
		public static Texture WhiteTexture { get; internal set; }
		public List<Renderable2D> RenderableList { get; internal set; } = new List<Renderable2D>();
		public RenderStats Stats { get; } = new RenderStats();
		public Framebuffer ActiveFramebuffer { get; set; }
		internal Framebuffer AlternateFramebuffer { get; set; }
		internal Framebuffer MainFramebuffer { get; set; }
		public bool RenderToFramebuffer
		{
			get { return renderToFramebuffer; }
			set
			{
				if (value)
				{
					ActivePipeline = FramebufferPipeline;
					ActiveFramebuffer = AlternateFramebuffer;
				}
				else
				{
					ActivePipeline = MainPipeline;
					ActiveFramebuffer = MainFramebuffer;
				}
				renderToFramebuffer = value;
			}
		}

		protected override void Dispose(bool disposing)
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			if (disposing)
			{
				IndexBuffer.Dispose();
				VertexBuffer.Dispose();
				InstancesVertexBuffer.Dispose();
				CameraBuffer.Dispose();
				MainPipeline.Dispose();
				FramebufferPipeline.Dispose();
				if (!ActivePipeline.IsDisposed)
					ActivePipeline.Dispose();
				ResourceLayout.Dispose();
				for (int i = 0; i < Shaders.Length; i++)
					Shaders[i].Dispose();
				foreach (var kv in DataPerTexture)
				{
					kv.Value.ResourceSet.Dispose();
					kv.Key.Dispose();
				}
			}
		}
	}

	public class InstancingTextureData
	{
		public InstancingTextureData() { }

		public InstancingTextureData(ResourceSet resourceSet, uint count, uint indexStart)
		{
			ResourceSet = resourceSet;
			Count = count;
			IndexStart = indexStart;
		}

		public ResourceSet ResourceSet { get; set; }
		public uint Count { get; set; }
		public uint IndexStart { get; set; }
		public uint LastInstanceIndex => IndexStart + Count;
	}

	public class RenderStats
	{
		public int DrawCalls { get; set; }
	}
}
