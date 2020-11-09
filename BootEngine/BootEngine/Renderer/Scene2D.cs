using BootEngine.Utils.ProfilingTools;
using System.Collections.Generic;
using Veldrid;

namespace BootEngine.Renderer
{
	public sealed class Scene2D : Scene
	{
		public static Texture WhiteTexture { get; set; }
		public DeviceBuffer IndexBuffer { get; set; }
		public DeviceBuffer VertexBuffer { get; set; }
		public DeviceBuffer InstancesVertexBuffer { get; set; }
		public DeviceBuffer CameraBuffer { get; set; }
		public ResourceLayout ResourceLayout { get; set; }
		public Dictionary<Texture, InstancingTextureData> DataPerTexture { get; set; } = new Dictionary<Texture, InstancingTextureData>();
		public Pipeline Pipeline { get; set; }
		public Shader[] Shaders { get; set; }
		public List<Renderable2D> RenderableList { get; internal set; } = new List<Renderable2D>();
		public RenderStats Stats { get; } = new RenderStats();

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
				Pipeline.Dispose();
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
