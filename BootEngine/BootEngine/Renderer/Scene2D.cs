using Veldrid;

namespace BootEngine.Renderer
{
	public sealed class Scene2D : Scene
	{
		public DeviceBuffer IndexBuffer { get; set; }
		public DeviceBuffer VertexBuffer { get; set; }
		public DeviceBuffer CameraBuffer { get; set; }
		public ResourceLayout ResourceLayout { get; set; }
		public Pipeline Pipeline { get; set; }
		public Shader[] Shaders { get; set; }

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				IndexBuffer.Dispose();
				VertexBuffer.Dispose();
				CameraBuffer.Dispose();
				Pipeline.Dispose();
				ResourceLayout.Dispose();
				for (int i = 0; i < Shaders.Length; i++)
					Shaders[i].Dispose();
			}
			base.Dispose(disposing);
		}
	}
}
