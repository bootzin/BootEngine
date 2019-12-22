using System;
using System.Collections.Generic;

namespace BootEngine.Renderer
{
	public abstract class Scene : IDisposable
	{
		public List<Renderable> RenderableList { get; } = new List<Renderable>();

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				for (int i = 0; i < RenderableList.Count; i++)
					RenderableList[i].Dispose();
			}
		}
	}
}
