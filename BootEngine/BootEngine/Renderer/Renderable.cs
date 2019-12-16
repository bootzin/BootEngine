using System;

namespace BootEngine.Renderer
{
	public abstract class Renderable : IDisposable
	{
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected abstract void Dispose(bool disposing);

		~Renderable()
		{
			Dispose(false);
		}
	}
}
