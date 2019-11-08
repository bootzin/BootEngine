using BootEngine.Input;
using System.Numerics;
using Utils;

namespace BootEngine.Renderer
{
	public class OrthoCamera : ICamera
	{
		private Vector3 position;
		private float rotation;
		private Matrix4x4 projectionMatrix;
		private Matrix4x4 viewMatrix;
		private Matrix4x4 viewProjectionMatrix;

		public Vector3 Position { get { return position; } set { position = value; UpdateViewMatrix(); } }
		public float Rotation { get => rotation; set { rotation = value; UpdateViewMatrix(); } }

		public ref readonly Matrix4x4 ProjectionMatrix => ref projectionMatrix;
		public ref readonly Matrix4x4 ViewMatrix => ref viewMatrix;
		public ref readonly Matrix4x4 ViewProjectionMatrix => ref viewProjectionMatrix;

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
		}

		private void UpdateViewMatrix()
		{
			Matrix4x4 transform = Matrix4x4.CreateTranslation(Position) * Matrix4x4.CreateRotationZ(Rotation);
			Matrix4x4.Invert(transform, out viewMatrix);
			viewProjectionMatrix = ViewMatrix * ProjectionMatrix;
		}

		public void Update()
		{
			InputManager inputManager = InputManager.Instance;

			Vector3 dir = Vector3.Zero;
			if (inputManager.GetKeyDown(KeyCodes.A))
			{
				dir -= Vector3.UnitX;
			}
			else if (inputManager.GetKeyDown(KeyCodes.D))
			{
				dir += Vector3.UnitX;
			}
			if (inputManager.GetKeyDown(KeyCodes.S))
			{
				dir -= Vector3.UnitY;
			}
			else if (inputManager.GetKeyDown(KeyCodes.W))
			{
				dir += Vector3.UnitY;
			}

			float rot = 0f;
			if (inputManager.GetKeyDown(KeyCodes.Q))
			{
				rot += (float)Util.Deg2Rad(5);
			}
			else if (inputManager.GetKeyDown(KeyCodes.E))
			{
				rot -= (float)Util.Deg2Rad(5);
			}

			Position += dir * .1f;
			Rotation += rot;
		}
	}
}
