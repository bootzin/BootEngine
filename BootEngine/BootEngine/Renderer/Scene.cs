using System;
using System.Collections.Generic;
using System.Text;
using Veldrid;

namespace BootEngine.Renderer
{
	public abstract class Scene
	{
		public Renderable[] RenderableList { get; set; }
		public OrthoCamera Camera { get; }
	}
}
