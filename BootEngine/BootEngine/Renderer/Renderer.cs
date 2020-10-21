using BootEngine.Utils;
using Veldrid;

namespace BootEngine.Renderer
{
	public abstract class Renderer<T> : Singleton<T> where T : Singleton<T>
	{
		protected abstract void BeginRender(CommandList cl);
		protected abstract void InnerRender(Renderable renderable, CommandList cl);
		protected abstract void EndRender(CommandList cl);

		protected void Render(Scene scene, CommandList cl)
		{
			BeginRender(cl);
			for (int i = scene.RenderableList.Count; i > 0;)
				InnerRender(scene.RenderableList[--i], cl);
			EndRender(cl);
		}
	}
}
