using BootEngine.Utils.ProfilingTools;
using System.Numerics;
using Veldrid;

namespace BootEngine.Renderer
{
	public class Renderable2D : Renderable
	{
		public Texture Texture { get; set; }
		public Vector4 Color { get; set; }
		public Vector3 Position { get; set; }
		public Vector2 Size { get; set; }
		public float Rotation { get; set; }

		internal ResourceSet ResourceSet { get; set; }

		public void SetParameters(ref Renderable2DParameters parameters)
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			Color = parameters.Color;
			Position = parameters.Position;
			Rotation = parameters.Rotation;
			Size = parameters.Size;
			Name = parameters.Name;
		}

		protected override void Dispose(bool disposing)
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			if (disposing)
			{
				Texture?.Dispose();
				ResourceSet.Dispose();
			}
		}
	}
}
