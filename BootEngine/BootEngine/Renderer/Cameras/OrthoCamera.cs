namespace BootEngine.Renderer.Cameras
{
	public sealed class OrthoCamera : Camera
	{
		public OrthoCamera() { }
		public OrthoCamera(float size, float nearClip, float farClip, int width, int height)
		{
			SetOrthographic(size, nearClip, farClip);
			ResizeViewport(width, height);
		}
	}
}
