using BootEngine.Utils.ProfilingTools;
using System.Numerics;

namespace BootEngine.Renderer.Cameras
{
	public sealed class OrthoCamera : Camera
	{
		private Vector3 position;
		private float rotation;
		private Matrix4x4 projectionMatrix;
		private Matrix4x4 viewMatrix;
		private Matrix4x4 viewProjectionMatrix;
		private readonly bool useReverseDepth;
		private readonly bool swapYAxis;

		public Vector3 Position { get { return position; } set { position = value; UpdateViewMatrix(); } }
		public float Rotation { get => rotation; set { rotation = value; UpdateViewMatrix(); } }

		public ref readonly Matrix4x4 ProjectionMatrix => ref projectionMatrix;
		public ref readonly Matrix4x4 ViewMatrix => ref viewMatrix;
		public ref readonly Matrix4x4 ViewProjectionMatrix => ref viewProjectionMatrix;

		public OrthoCamera(float left, float right, float bottom, float top, bool useReverseDepth = false, bool swapYAxis = false)
		{
			this.useReverseDepth = useReverseDepth;
			this.swapYAxis = swapYAxis;
			viewMatrix = Matrix4x4.Identity;
			SetProjectionMatrix(left, right, bottom, top);
		}

		public void SetProjectionMatrix(float left, float right, float bottom, float top)
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			if (useReverseDepth)
			{
				projectionMatrix = Matrix4x4.CreateOrthographicOffCenter(left, right, bottom, top, 1f, -1f);
			}
			else
			{
				projectionMatrix = Matrix4x4.CreateOrthographicOffCenter(left, right, bottom, top, -1f, 1f);
			}
			if (swapYAxis)
			{
				projectionMatrix *= new Matrix4x4(
					1, 0, 0, 0,
					0, -1, 0, 0,
					0, 0, 1, 0,
					0, 0, 0, 1);
			}
			viewProjectionMatrix = ViewMatrix * ProjectionMatrix;
		}

		private void UpdateViewMatrix()
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			Matrix4x4 transform = Matrix4x4.CreateTranslation(Position) * Matrix4x4.CreateRotationZ(Rotation);
			Matrix4x4.Invert(transform, out viewMatrix);
			viewProjectionMatrix = ViewMatrix * ProjectionMatrix;
		}
	}
}
