using System;
using System.Numerics;
using Veldrid;

namespace BootEngine.Renderer
{
	public sealed class RenderData2D : IDisposable
	{
		private static readonly GraphicsDevice _gd = Application.App.Window.GraphicsDevice;

		public RenderData2D(ushort[] indices, Vertex2D[] vertices, Texture texture)
		{
			Indices = indices;
			Vertices = vertices;
			Texture = texture;
			GenerateIndexBuffer();
			GenerateVertexBuffer();
		}

		public Texture Texture { get; set; }

		public ushort[] Indices { get; }
		internal DeviceBuffer IndexBuffer { get; private set; }

		private void GenerateIndexBuffer()
		{
			IndexBuffer = _gd.ResourceFactory.CreateBuffer(new BufferDescription((uint)(Indices.Length * sizeof(ushort)), BufferUsage.IndexBuffer));
			_gd.UpdateBuffer(IndexBuffer, 0, Indices);
		}

		public Vertex2D[] Vertices { get; }
		internal DeviceBuffer VertexBuffer { get; private set; }

		private void GenerateVertexBuffer()
		{
			VertexBuffer = _gd.ResourceFactory.CreateBuffer(new BufferDescription((uint)(Vertices.Length * Vertex2D.SizeInBytes), BufferUsage.VertexBuffer));
			_gd.UpdateBuffer(VertexBuffer, 0, Vertices);
		}

		public void Dispose()
		{
			IndexBuffer.Dispose();
			VertexBuffer.Dispose();
		}

		public override bool Equals(object obj)
		{
			if (obj is RenderData2D other)
			{
				return Indices == other.Indices
					&& Vertices == other.Vertices
					&& Texture == other.Texture;
			}
			return false;
		}

		public override int GetHashCode()
		{
			int hash = 12;
			foreach (var i in Indices)
			{
				hash += Indices[i].GetHashCode();
			}
			foreach (var i in Indices)
			{
				hash += Vertices[i].GetHashCode();
			}
			if (Texture != null)
				hash += Texture.Name.GetHashCode();
			return hash;
		}

		public static RenderData2D QuadData => new RenderData2D(
			new ushort[] { 0, 1, 2, 3 }, // TODO: Change to triangle list
			new Vertex2D[]
			{
				new Vertex2D(new Vector3(-.5f, .5f, 0f), new Vector2(0.0f, 1.0f)),
				new Vertex2D(new Vector3(.5f, .5f, 0f), new Vector2(1.0f, 1.0f)),
				new Vertex2D(new Vector3(-.5f, -.5f, 0f), new Vector2(0.0f, 0.0f)),
				new Vertex2D(new Vector3(.5f, -.5f, 0f), new Vector2(1.0f, 0.0f))
			},
			Renderer2D.WhiteTexture);
	}
}
