using BootEngine.Renderer;
using BootEngine.Utils;

namespace BootEngine.ECS.Components
{
	public struct SpriteRendererComponent
	{
		private ColorF? color;

		public SpriteRendererComponent(ColorF color, Material material, RenderData2D spriteData)
		{
			this.color = color;
			Material = material;
			SpriteData = spriteData;
		}

		public ColorF Color
		{
			get { return color ?? (color = ColorF.White).Value; }
			set { color = value; }
		}

		public Material Material { get; set; }
		public RenderData2D SpriteData { get; set; }
	}
}
