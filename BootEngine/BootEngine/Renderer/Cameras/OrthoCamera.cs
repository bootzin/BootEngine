namespace BootEngine.Renderer.Cameras
{
	public sealed class OrthoCamera : Camera
	{
		public OrthoCamera(float size, float nearClip, float farClip, int width, int height) : base(false)
		{
			SetOrthographic(size, nearClip, farClip);
			ResizeViewport(width, height);
		}
	}
}
