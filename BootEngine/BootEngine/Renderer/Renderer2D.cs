using System;
using Veldrid;

namespace BootEngine.Renderer
{
	public sealed class Renderer2D : Renderer
	{
		public Renderer2D(CommandList cl, GraphicsDevice gd) : base(cl, gd)
		{
		}

		protected override void BeginRender()
		{
			throw new NotImplementedException();
		}

		public override void Render(Renderable renderable)
		{
			throw new NotImplementedException();
		}

		protected override void EndRender()
		{
			throw new NotImplementedException();
		}
	}
}
