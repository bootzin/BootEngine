using Veldrid;

namespace BootEngine.Renderer
{
	public class Renderable2D : Renderable
	{
		public DeviceBuffer ColorBuffer { get; set; }
		public DeviceBuffer TransformBuffer { get; set; }
		public ResourceSet ResourceSet { get; set; }

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				ColorBuffer.Dispose();
				TransformBuffer.Dispose();
				ResourceSet.Dispose();
			}
		}
	}
}
