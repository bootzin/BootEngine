using System.Numerics;

namespace BootEngine.ECS.Components
{
	public struct VelocityComponent
	{
		public Vector3 Velocity { get; set; }
		public Vector3 RotationSpeed { get; set; }
	}
}
