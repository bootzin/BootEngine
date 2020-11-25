using BootEngine.Utils;
using System.Numerics;

namespace BootEngine.Renderer.Cameras
{
	public abstract class Camera
	{
		protected Matrix4x4 projectionMatrix;
		public bool Active { get; set; } = true;
		public ProjectionType ProjectionType
		{
			get { return projectionType; }
			set { projectionType = value; RecalculateProjection(); }
		}
		public ref readonly Matrix4x4 ProjectionMatrix => ref projectionMatrix;
		public float ZoomLevel
		{
			get { return zoomLevel; }
			set { zoomLevel = value; RecalculateProjection(); }
		}

		protected float aspectRatio;
		private float zoomLevel = 1f;

		public float PerspectiveFov
		{
			get { return perspectiveFov; }
			set
			{
				perspectiveFov = value;
				RecalculateProjection();
			}
		}
		public float PerspectiveNear
		{
			get { return perspectiveNear; }
			set
			{
				perspectiveNear = value;
				RecalculateProjection();
			}
		}
		public float PerspectiveFar
		{
			get { return perspectiveFar; }
			set
			{
				perspectiveFar = value;
				RecalculateProjection();
			}
		}

		public float OrthoSize
		{
			get { return orthoSize; }
			set
			{
				orthoSize = value;
				RecalculateProjection();
			}
		}
		public float OrthoNear
		{
			get { return orthoNear; }
			set
			{
				orthoNear = value;
				RecalculateProjection();
			}
		}
		public float OrthoFar
		{
			get { return orthoFar; }
			set
			{
				orthoFar = value;
				RecalculateProjection();
			}
		}

		protected readonly bool useReverseDepth = Application.App.Window.GraphicsDevice.IsDepthRangeZeroToOne;
		protected readonly bool swapYAxis = Application.App.Window.GraphicsDevice.IsClipSpaceYInverted;

		private float perspectiveFov = Util.Deg2Rad(45);
		private float perspectiveNear = 0.01f;
		private float perspectiveFar = 1000f;
		private float orthoSize = 1f;
		private float orthoFar = 1;
		private float orthoNear = -1;
		private ProjectionType projectionType;

		public void ResizeViewport(int width, int height)
		{
			aspectRatio = (float)width / height;
			RecalculateProjection();
		}

		public void SetOrthographic(float size, float nearClip, float farClip)
		{
			ProjectionType = ProjectionType.Orthographic;
			OrthoSize = size;
			OrthoNear = nearClip;
			OrthoFar = farClip;
			RecalculateProjection();
		}

		public void SetPerspective(float verticalFOV, float nearClip, float farClip)
		{
			ProjectionType = ProjectionType.Perspective;
			PerspectiveFov = verticalFOV;
			PerspectiveNear = nearClip;
			PerspectiveFar = farClip;
			RecalculateProjection();
		}

		// TODO: implement perspective camera
		protected virtual void RecalculateProjection()
		{
			if (ProjectionType == ProjectionType.Perspective)
			{
				projectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(PerspectiveFov, aspectRatio, PerspectiveNear, PerspectiveFar);
			}
		}
	}
}