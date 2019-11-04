using System;
using System.Collections.Generic;
using System.Text;
using Veldrid;

namespace BootEngine.Renderer
{
	public abstract class Renderer
	{
		protected Scene currentScene;
		protected CommandList CommandList { get; }
		protected GraphicsDevice GraphicsDevice { get; }

		protected Renderer(CommandList cl, GraphicsDevice gd)
		{
			CommandList = cl;
			GraphicsDevice = gd;
		}

		public abstract void Render(Renderable renderable);

		public void Render(Scene scene)
		{
			foreach (Renderable renderable in scene.RenderableList)
			{
				BeginRender();
				Render(renderable);
				EndRender();
			}
		}

		protected abstract void BeginRender();

		protected abstract void EndRender();
	}
}
