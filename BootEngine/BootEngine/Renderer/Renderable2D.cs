using BootEngine.Utils.ProfilingTools;
using System.Numerics;
using Veldrid;

namespace BootEngine.Renderer
{
	public sealed class Renderable2D : Renderable
	{
		#region Constructors
		public Renderable2D() { }

		public Renderable2D(ref Renderable2DParameters parameters)
		{
			Name = parameters.Name;
			Position = parameters.Position;
			Size = parameters.Size;
			Rotation = parameters.Rotation;
			Color = parameters.Color;
			Texture = parameters.Texture;
		}

		public Renderable2D(ref Renderable2DParameters parameters, int instanceIndex)
		{
			Name = parameters.Name;
			Position = parameters.Position;
			Size = parameters.Size;
			Rotation = parameters.Rotation;
			Color = parameters.Color;
			Texture = parameters.Texture;
			InstanceIndex = instanceIndex;
		}

		public Renderable2D(Vector3 pos, Vector2 scale, float rot, Vector4 col)
		{
			Position = pos;
			Size = scale;
			Rotation = rot;
			Color = col;
		}

		public Renderable2D(string name, Vector3 pos, Vector2 scale, float rot, Vector4 col)
		{
			Name = name;
			Position = pos;
			Size = scale;
			Rotation = rot;
			Color = col;
		}

		public Renderable2D(Vector3 pos, Vector2 scale, float rot, Vector4 col, Texture tex)
		{
			Position = pos;
			Size = scale;
			Rotation = rot;
			Color = col;
			Texture = tex;
		}

		public Renderable2D(string name, Vector3 pos, Vector2 scale, float rot, Vector4 col, Texture tex)
		{
			Name = name;
			Position = pos;
			Size = scale;
			Rotation = rot;
			Color = col;
			Texture = tex;
		}
		#endregion

		public Vector3 Position { get; set; }
		public Vector2 Size { get; set; }
		public float Rotation { get; set; }
		public Vector4 Color { get; set; }
		public Texture Texture { get; set; }
		public int InstanceIndex { get; set; }

		public void SetParameters(ref Renderable2DParameters parameters)
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			Color = parameters.Color;
			Position = parameters.Position;
			Rotation = parameters.Rotation;
			Size = parameters.Size;
			Name = parameters.Name;
		}
	}
}
