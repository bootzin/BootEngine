using System.Numerics;

namespace BootEngine.ECS.Components
{
	public struct TransformComponent
	{
		private Vector3? scale;

		public Vector3 Translation { get; set; }

		// angles in radians
		public Vector3 Rotation { get; set; }
		public Vector3 Scale
		{
			get { return scale ?? Vector3.One; }
			set { scale = value; }
		}

		public TransformComponent(Vector3 position)
		{
			Translation = position;
			Rotation = Vector3.Zero;
			scale = Vector3.One;
		}

		public Matrix4x4 Transform
		{
			get
			{
				Matrix4x4 rotation = Matrix4x4.CreateRotationX(Rotation.X)
					* Matrix4x4.CreateRotationY(Rotation.Y)
					* Matrix4x4.CreateRotationZ(Rotation.Z);
				return Matrix4x4.CreateTranslation(Translation) * rotation * Matrix4x4.CreateScale(Scale);
			}
		}
	}
}
