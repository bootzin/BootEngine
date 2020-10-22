using BootEngine.Utils.ProfilingTools;
using System.Numerics;
using Veldrid;

namespace BootEngine.Renderer
{
	public sealed class Scene2D : Scene
	{
		public static Texture WhiteTexture { get; set; }
		public DeviceBuffer IndexBuffer { get; set; }
		public DeviceBuffer VertexBuffer { get; set; }
		public DeviceBuffer CameraBuffer { get; set; }
		public ResourceLayout ResourceLayout { get; set; }
		public Pipeline Pipeline { get; set; }
		public Shader[] Shaders { get; set; }

		public const int MaxQuads = 10_000;
		public const int MaxVertices = MaxQuads * 4;
		public const int MaxIndices = MaxQuads * 6;

		public uint IndexCount { get; set; }
		public QuadVertex[] QuadVertexBufferBase { get; set; }
		public int CurrentQuadVertex { get; set; }
		public Vector3[] QuadVertexPositions { get; set; }

		protected override void Dispose(bool disposing)
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			if (disposing)
			{
				IndexBuffer.Dispose();
				VertexBuffer.Dispose();
				CameraBuffer.Dispose();
				Pipeline.Dispose();
				ResourceLayout.Dispose();
				WhiteTexture.Dispose();
				for (int i = 0; i < Shaders.Length; i++)
					Shaders[i].Dispose();
			}
			base.Dispose(disposing);
		}
	}
}
