using BootEngine.Utils;
using System.Collections;
using System.Linq;
using Veldrid;

namespace BootEngine.Renderer
{
	public abstract class Renderer<T> : Singleton<T> where T : Singleton<T>
	{
		protected abstract void BeginRender(CommandList cl);
		protected abstract void InnerRender(CommandList cl, Pipeline pipeline);
		protected abstract void EndRender(CommandList cl);

		public void Render(CommandList cl, IDictionary pipelines)
		{
			BeginRender(cl);
			foreach (Pipeline pipeline in pipelines.Values)
			{
				InnerRender(cl, pipeline);
			}
			EndRender(cl);
		}
	}
}
