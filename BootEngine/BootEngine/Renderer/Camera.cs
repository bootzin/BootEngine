using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace BootEngine.Renderer
{
	public class Camera
	{
		public Matrix4x4 ProjectionMatrix { get; set; }
		public Matrix4x4 ViewMatrix { get; set; }

		public Camera(float left, float right, float bottom, float top )
		{
			ProjectionMatrix = Matrix4x4.CreateOrthographicOffCenter(left, right, bottom, top, -1.0f, 1.0f);
		}
	}
}
