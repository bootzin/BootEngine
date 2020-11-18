using System.Numerics;

namespace BootEngine.Renderer.Cameras
{
	public abstract class Camera
	{
		protected Matrix4x4 projectionMatrix;
		public bool Active { get; set; } = true;
		public ref readonly Matrix4x4 ProjectionMatrix => ref projectionMatrix;
	}
}