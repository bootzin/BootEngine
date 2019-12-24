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
		public Dictionary<Texture, ResourceSet> ResourceSetsPerTexture { get; set; } = new Dictionary<Texture, ResourceSet>();
		public Dictionary<Texture, uint> InstancesPerTexture { get; set; } = new Dictionary<Texture, uint>();
		public Pipeline Pipeline { get; set; }
		public Shader[] Shaders { get; set; }

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
				WhiteTexture.Dispose();
				for (int i = 0; i < Shaders.Length; i++)
					Shaders[i].Dispose();
				foreach (var kv in ResourceSetsPerTexture)
				{
					kv.Value.Dispose();
					kv.Key.Dispose();
				}
			}
		}
	}
}
