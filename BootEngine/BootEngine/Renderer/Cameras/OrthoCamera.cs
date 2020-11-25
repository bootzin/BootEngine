using BootEngine.Utils.ProfilingTools;
using System.Numerics;

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

		protected override void RecalculateProjection()
		{
			if (ProjectionType == ProjectionType.Orthographic)
			{
#if DEBUG
				using Profiler fullProfiler = new Profiler(GetType());
#endif
				float left = -OrthoSize * aspectRatio * ZoomLevel;
				float right = OrthoSize * aspectRatio * ZoomLevel;
				float bottom = -OrthoSize * ZoomLevel;
				float top = OrthoSize * ZoomLevel;

				if (useReverseDepth)
				{
					projectionMatrix = Matrix4x4.CreateOrthographicOffCenter(left, right, bottom, top, OrthoFar, OrthoNear);
				}
				else
				{
					projectionMatrix = Matrix4x4.CreateOrthographicOffCenter(left, right, bottom, top, OrthoNear, OrthoFar);
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
			else
			{
				base.RecalculateProjection();
			}
		}
	}
}
