using System.Numerics;

namespace BootEngine.Renderer
{
	public class OrthoCamera : ICamera
	{
		private Vector3 position;
		private float rotation;
		private Matrix4x4 projectionMatrix;
		private Matrix4x4 viewMatrix;
		private Matrix4x4 viewProjectionMatrix;

		public Vector3 Position { get => position; set { position = value; UpdateViewMatrix(); } }
		public float Rotation { get => rotation; set { rotation = value; UpdateViewMatrix(); } }

		public ref readonly Matrix4x4 ProjectionMatrix => ref projectionMatrix;
		public ref readonly Matrix4x4 ViewMatrix => ref viewMatrix;
		public ref readonly Matrix4x4 ViewProjectionMatrix => ref viewProjectionMatrix;

		public Matrix4x4 OpenGLMatrix;

		public OrthoCamera(float left, float right, float bottom, float top, bool useReverseDepth = false, bool isClipSpaceYInverted = false)
		{
			if (useReverseDepth)
			{
				projectionMatrix = Matrix4x4.CreateOrthographicOffCenter(left, right, bottom, top, 1f, -1f);
			}
			else
			{
				projectionMatrix = Matrix4x4.CreateOrthographicOffCenter(left, right, bottom, top, -1f, 1f);
			}
			if (isClipSpaceYInverted)
			{
				projectionMatrix *= new Matrix4x4(
					1, 0, 0, 0,
					0, -1, 0, 0,
					0, 0, 1, 0,
					0, 0, 0, 1);
			}
			viewMatrix = Matrix4x4.Identity;
			viewProjectionMatrix = ViewMatrix * ProjectionMatrix;
			OpenGLMatrix = new Matrix4x4(
				1/right, 0, 0, 0,
				0, 1/top, 0, 0,
				0, 0, -2/2, 0,
				0, 0, 0, 1);
		}

		private void UpdateViewMatrix()
		{
			Matrix4x4 transform = Matrix4x4.CreateTranslation(Position) * Matrix4x4.CreateRotationZ(Rotation);
			Matrix4x4.Invert(transform, out viewMatrix);
			viewProjectionMatrix = ViewMatrix * ProjectionMatrix;
		}
	}
}
