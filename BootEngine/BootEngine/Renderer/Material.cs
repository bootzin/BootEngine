using BootEngine.Utils;
using System.Numerics;
using Veldrid;

namespace BootEngine.Renderer
{
	public sealed class Material
	{
		public Material(string shaderSetName)
		{
			ShaderSetName = shaderSetName;
		}

		public string Name { get; set; } = "Default";
		public MaterialRenderingMode RenderingMode { get; set; }
		public string ShaderSetName { get; set; }
		public ColorF Color { get; set; } = ColorF.White;
		public Texture Albedo { get; set; }
		public Texture NormalMap { get; set; }
		public Texture HeightMap { get; set; }
		public Texture Occlusion { get; set; }
		public Vector2 Tiling { get; set; } = Vector2.One;
		public Vector2 Offset { get; set; }
	}

	public enum MaterialRenderingMode
	{
		Opaque,
		Transparent,
		//Cutout
	}
}
