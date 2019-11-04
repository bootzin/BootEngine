using System;
using System.Collections.Generic;
using System.Text;
using Veldrid;

namespace BootEngine.Renderer
{
	public class Scene
	{
		public Renderable[] RenderableList { get; set; }
		public Camera Camera { get; set; }
	}
}
