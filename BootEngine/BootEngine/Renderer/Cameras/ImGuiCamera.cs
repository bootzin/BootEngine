﻿using BootEngine.Utils.ProfilingTools;
using System.Numerics;

namespace BootEngine.Renderer.Cameras
{
	//TODO: Check the viability of merging this with OrthoCamera
	internal sealed class ImGuiCamera : Camera
	{
		private readonly float left;
		private readonly float right;
		private readonly float bottom;
		private readonly float top;

		public ImGuiCamera(float left, float right, float bottom, float top)
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