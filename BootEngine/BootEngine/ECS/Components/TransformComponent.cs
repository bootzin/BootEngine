using System.Numerics;

namespace BootEngine.ECS.Components
{
	public struct TransformComponent
	{
		private Vector3? scale;

		// angles in radians
		public Vector3 Rotation { get; set; }
		public Vector3 Translation { get; set; }
		public Vector3 Scale
		{
			get { return scale ?? Vector3.One; }
			set { scale = value; }
		}

		public Matrix4x4 Transform
		{
			get
			{
				Matrix4x4 rotation = Matrix4x4.CreateFromYawPitchRoll(Rotation.Y, Rotation.X, Rotation.Z);
				return Matrix4x4.CreateTranslation(Translation) * rotation * Matrix4x4.CreateScale(Scale);
			}
		}
	}
}
