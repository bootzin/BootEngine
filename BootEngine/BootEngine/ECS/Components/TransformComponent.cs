using System.Numerics;

namespace BootEngine.ECS.Components
{
	public struct TransformComponent
	{
		public Matrix4x4 Transform { get; set; }
		public TransformComponent(Matrix4x4 transform)
		{
			Transform = transform;
		}
	}
}
