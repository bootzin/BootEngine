using BootEngine.Utils;
using BootEngine.Utils.ProfilingTools;
using System;
using System.Numerics;
using Veldrid;

namespace BootEngine.Renderer.Cameras
{
	public class Camera : IDisposable
	{
		protected float aspectRatio;
		protected Matrix4x4 projectionMatrix;
		protected readonly bool useReverseDepth = Application.App.Window.GraphicsDevice.IsDepthRangeZeroToOne;
		protected readonly GraphicsDevice _gd = Application.App.Window.GraphicsDevice;

		public readonly bool SwapYAxis = Application.App.Window.GraphicsDevice.IsClipSpaceYInverted;
		public ref readonly Matrix4x4 ProjectionMatrix => ref projectionMatrix;

		public int ViewportWidth { get; protected set; }
		public int ViewportHeight { get; protected set; }
		public bool Active { get; set; } = true;

		#region RenderingData
		public BlendStateDescription BlendState { get; set; } = BlendStateDescription.SingleAlphaBlend;
		public DepthStencilStateDescription DepthStencilState { get; set; } = DepthStencilStateDescription.DepthOnlyLessEqual;
		public RasterizerStateDescription RasterizerState { get; set; } = RasterizerStateDescription.Default;
		public Framebuffer RenderTarget { get; set; }
		public Texture DepthTarget { get; set; }
		public Texture[] ColorTargets { get; set; }
		#endregion

		#region Ortho/Perspective properties
		public ProjectionType ProjectionType
		{
			get { return projectionType; }
			set { projectionType = value; RecalculateProjection(); }
		}
		public float ZoomLevel
		{
			get { return zoomLevel; }
			set { zoomLevel = value; RecalculateProjection(); }
		}

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

		private float perspectiveFov = MathUtil.Deg2Rad(45);
		private float perspectiveNear = 0.01f;
		private float perspectiveFar = 1000f;
		private float orthoSize = 1f;
		private float orthoFar = 1;
		private float orthoNear = -1;
		private float zoomLevel = 1f;
		private ProjectionType projectionType;
		private bool disposed;
		#endregion

		public Camera(bool generateRenderTarget)
		{
			if (generateRenderTarget)
				GenerateDefaultRenderTarget();
		}

		private void GenerateDefaultRenderTarget()
		{
			ColorTargets = new Texture[]
			{
				_gd.ResourceFactory.CreateTexture(TextureDescription.Texture2D(
					(uint)Application.App.Window.SdlWindow.Width, // Width
					(uint)Application.App.Window.SdlWindow.Height, // Height
					1,  // Miplevel
					1,  // ArrayLayers
					PixelFormat.R8_G8_B8_A8_UNorm,
					TextureUsage.RenderTarget | TextureUsage.Sampled))
			};

			DepthTarget = _gd.ResourceFactory.CreateTexture(TextureDescription.Texture2D(
				(uint)Application.App.Window.SdlWindow.Width, // Width
				(uint)Application.App.Window.SdlWindow.Height, // Height
				1,  // Miplevel
				1,  // ArrayLayers
				PixelFormat.R16_UNorm,
				TextureUsage.DepthStencil));

			RenderTarget = _gd.ResourceFactory.CreateFramebuffer(new FramebufferDescription(DepthTarget, ColorTargets));
		}

		public void ResizeViewport(int width, int height)
		{
			ViewportWidth = width;
			ViewportHeight = height;
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

		protected virtual void RecalculateProjection()
		{
			if (ProjectionType == ProjectionType.Perspective)
			{
				projectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(PerspectiveFov, aspectRatio, PerspectiveNear, PerspectiveFar);
			}
			else
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

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!disposed)
			{
				if (disposing)
				{
					DepthTarget.Dispose();
					foreach (var ct in ColorTargets)
					{
						ct.Dispose();
					}
					RenderTarget.Dispose();
				}
				disposed = true;
			}
		}
	}
}