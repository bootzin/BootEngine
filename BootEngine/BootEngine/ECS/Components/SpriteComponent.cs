using BootEngine.Utils;
using Veldrid;

namespace BootEngine.ECS.Components
{
	public struct SpriteComponent
	{
		private ColorF? color;

		public ColorF Color
		{
			get { return color ?? ColorF.White; }
			set { color = value; }
		}
		public Texture Texture { get; set; }

		public SpriteComponent(ColorF color)
		{
			this.color = color;
			Texture = null;
		}

		public SpriteComponent(Texture tex)
		{
			this.color = ColorF.White;
			Texture = tex;
		}

		public SpriteComponent(Texture tex, ColorF color)
		{
			this.color = color;
			Texture = tex;
		}
	}
}
