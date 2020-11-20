using BootEngine.Utils.ProfilingTools;
using System.Numerics;

namespace BootEngine.Renderer.Cameras
{
	public sealed class OrthoCamera : Camera
	{
		public OrthoCamera(float size, float nearClip, float farClip)
		{
			SetOrthographic(size, nearClip, farClip);
		}

		protected override void RecalculateProjection()
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			float left = -orthoSize * aspectRatio * ZoomLevel;
			float right = orthoSize * aspectRatio * ZoomLevel;
			float bottom = -orthoSize * ZoomLevel;
			float top = orthoSize * ZoomLevel;

			if (useReverseDepth)
			{
				projectionMatrix = Matrix4x4.CreateOrthographicOffCenter(left, right, bottom, top, orthoFar, orthoNear);
			}
			else
			{
				projectionMatrix = Matrix4x4.CreateOrthographicOffCenter(left, right, bottom, top, orthoNear, orthoFar);
			}
			if (swapYAxis)
			{
				projectionMatrix *= new Matrix4x4(
					1, 0, 0, 0,
					0, -1, 0, 0,
					0, 0, 1, 0,
					0, 0, 0, 1);
			}
		}
	}
}
