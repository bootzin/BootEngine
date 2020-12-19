using BootEngine.Utils.ProfilingTools;
using System.Numerics;

namespace BootEngine.Renderer.Cameras
{
	//TODO: Check the viability of merging this with Camera
	internal sealed class ImGuiCamera : Camera
	{
		private readonly float left;
		private readonly float right;
		private readonly float bottom;
		private readonly float top;

		public ImGuiCamera(float left, float right, float bottom, float top) : base(false)
		{
			this.left = left;
			this.right = right;
			this.top = top;
			this.bottom = bottom;
			RecalculateProjection();
		}

		protected override void RecalculateProjection()
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			if (useReverseDepth)
			{
				projectionMatrix = Matrix4x4.CreateOrthographicOffCenter(left, right, bottom, top, OrthoFar, OrthoNear);
			}
			else
			{
				projectionMatrix = Matrix4x4.CreateOrthographicOffCenter(left, right, bottom, top, OrthoNear, OrthoFar);
			}
			if (SwapYAxis)
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
