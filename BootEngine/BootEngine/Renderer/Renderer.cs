using System;
using System.Collections.Generic;
using System.Text;

namespace BootEngine.Renderer
{
	public class Renderer
	{
		private Scene _currentScene;
		public void Render(Renderable renderable)
		{
			
		}

		public void Render(Scene scene)
		{
			_currentScene = scene;
			foreach (Renderable renderable in scene.RenderableList)
			{
				Render(renderable);
			}
		}
	}
}
