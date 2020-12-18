using BootEngine.Utils;
using Veldrid;

namespace BootEngine.Renderer
{
	public abstract class Renderer<T> : Singleton<T> where T : Singleton<T>
	{
		protected abstract void BeginRender(CommandList cl, Pipeline pipeline);
		protected abstract void InnerRender(CommandList cl);
		protected abstract void EndRender(CommandList cl);

		public void Render(CommandList cl, Pipeline pipeline)
		{
			BeginRender(cl, pipeline);
			InnerRender(cl);
			EndRender(cl);
		}
	}
}
