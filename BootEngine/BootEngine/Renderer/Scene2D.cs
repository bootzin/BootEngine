using BootEngine.Utils.ProfilingTools;
using System.Collections.Generic;
using System.Numerics;
using Veldrid;

namespace BootEngine.Renderer
{
	public sealed class Scene2D : Scene
	{
		public const int MaxQuads = 20_000;
		public const int MaxVertices = MaxQuads * 4;
		public const int MaxIndices = MaxQuads * 6;
		public const int MaxTextureSlots = 32;

		public static Texture WhiteTexture { get; set; }
		public DeviceBuffer IndexBuffer { get; set; }
		public DeviceBuffer VertexBuffer { get; set; }
		public DeviceBuffer CameraBuffer { get; set; }
		public ResourceLayout ResourceLayout { get; set; }
		public Pipeline Pipeline { get; set; }
		public Shader[] Shaders { get; set; }
		public Vector3[] QuadVertexPositions { get; set; }
		public Vector2[] QuadTexCoords { get; set; }
		public uint IndexCount { get; set; }
		public uint CurrentQuadVertex { get; set; }
		public int TextureIndex { get; set; } = 1; // 0 = white texture
		public QuadVertex[] QuadVertexBufferBase { get; set; } = new QuadVertex[MaxVertices];
		public List<Texture> TextureSlots { get; set; } = new List<Texture>(MaxTextureSlots);

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
