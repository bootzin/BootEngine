using BootEngine.Utils;
using Veldrid;

namespace BootEngine.Renderer
{
	public abstract class Renderer<T> : Singleton<T> where T : Singleton<T>
	{
		protected abstract void BeginRender(CommandList cl);
		protected abstract void BatchRender(CommandList cl);
		protected abstract void EndRender(CommandList cl);

		public void Render(CommandList cl)
		{
			BeginRender(cl);
			BatchRender(cl);
			EndRender(cl);
		}
	}
}
