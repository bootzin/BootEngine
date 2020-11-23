using BootEngine.Utils;
using System.Numerics;

namespace BootEngine.Renderer.Cameras
{
	public abstract class Camera
	{
		protected Matrix4x4 projectionMatrix;
		public bool Active { get; set; } = true;
		public ref readonly Matrix4x4 ProjectionMatrix => ref projectionMatrix;
		public float ZoomLevel
		{
			get { return zoomLevel; }
			set { zoomLevel = value; RecalculateProjection(); }
		}

		protected float aspectRatio;
		private ProjectionType ProjectionType;
		private float zoomLevel = 1f;

		protected float perspectiveFov = Util.Deg2Rad(45);
		protected float perspectiveNear = 0.01f;
		protected float perspectiveFar = 1000f;

		public float OrthoSize { get; private set; } = 1f;
		protected float orthoNear = -1;
		protected float orthoFar = 1;

		protected readonly bool useReverseDepth = Application.App.Window.GraphicsDevice.IsDepthRangeZeroToOne;
		protected readonly bool swapYAxis = Application.App.Window.GraphicsDevice.IsClipSpaceYInverted;

		public void ResizeViewport(int width, int height)
		{
			aspectRatio = (float)width / height;
			RecalculateProjection();
		}

		public void SetOrthographic(float size, float nearClip, float farClip)
		{
			ProjectionType = ProjectionType.Orthographic;
			OrthoSize = size;
			orthoNear = nearClip;
			orthoFar = farClip;
			RecalculateProjection();
		}

		public void SetPerspective(float verticalFOV, float nearClip, float farClip)
		{
			ProjectionType = ProjectionType.Perspective;
			perspectiveFov = verticalFOV;
			perspectiveNear = nearClip;
			perspectiveFar = farClip;
			RecalculateProjection();
		}

		// TODO: implement perspective camera
		protected virtual void RecalculateProjection()
		{
			if (ProjectionType == ProjectionType.Perspective)
			{
				projectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(perspectiveFov, aspectRatio, perspectiveNear, perspectiveFar);
			}
		}
	}
}