using BootEngine.Utils;
using Veldrid;

namespace BootEngine.ECS.Components
{
	public struct SpriteComponent
	{
		public ColorF Color { get; set; }
		public Texture Texture { get; set; }

		public SpriteComponent(ColorF color)
		{
			Color = color;
			Texture = null;
		}

		public SpriteComponent(Texture tex)
		{
			Color = ColorF.White;
			Texture = tex;
		}

		public SpriteComponent(Texture tex, ColorF color)
		{
			Color = color;
			Texture = tex;
		}
	}
}
