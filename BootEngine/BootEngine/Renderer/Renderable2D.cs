using BootEngine.Utils.ProfilingTools;
using Veldrid;

namespace BootEngine.Renderer
{
	public class Renderable2D : Renderable
	{
		public DeviceBuffer ColorBuffer { get; set; }
		public DeviceBuffer TransformBuffer { get; set; }
		public ResourceSet ResourceSet { get; set; }
		public Texture Texture { get; set; }

		protected override void Dispose(bool disposing)
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			if (disposing)
			{
				Texture?.Dispose();
				ColorBuffer.Dispose();
				TransformBuffer.Dispose();
				ResourceSet.Dispose();
			}
		}
	}
}
