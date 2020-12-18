using System.Numerics;

namespace BootEngine.Renderer
{
	public readonly struct Vertex2D
	{
		public const uint SizeInBytes = 20;

		public Vertex2D(Vector3 position, Vector2 texCoord)
		{
			Position = position;
			TexCoord = texCoord;
		}

		public readonly Vector3 Position { get; }
		public readonly Vector2 TexCoord { get; }
	}
}
