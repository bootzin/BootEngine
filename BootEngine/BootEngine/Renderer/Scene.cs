using System;

namespace BootEngine.Renderer
{
	public abstract class Scene : IDisposable
	{
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			//
		}
	}
}
