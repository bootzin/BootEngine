using BootEngine.Events;
using BootEngine.Input;
using BootEngine.Utils.ProfilingTools;
using System;
using System.Numerics;
using Utils;

namespace BootEngine.Renderer.Cameras
{
	public sealed class OrthoCameraController : IUpdatable, ICameraController
	{
		private const float CAMERA_MOVE_SPEED = 2f;
		private const float CAMERA_ROTATION_SPEED = 60f;

		private readonly bool _enableRotation;
		private float aspectRatio;
		private float zoomLevel = 1f;

		public OrthoCamera Camera { get; }

		public OrthoCameraController(OrthoCamera camera, float aspectRatio, bool enableRotation = true)
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			Camera = camera;
			this.aspectRatio = aspectRatio;
			_enableRotation = enableRotation;
		}

		public OrthoCameraController(float aspectRatio, bool enableRotation = true)
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			Camera = new OrthoCamera(-aspectRatio * zoomLevel, aspectRatio * zoomLevel, -zoomLevel, zoomLevel,
				Application.App.Window.GraphicsDevice.IsDepthRangeZeroToOne, Application.App.Window.GraphicsDevice.IsClipSpaceYInverted);
			this.aspectRatio = aspectRatio;
			_enableRotation = enableRotation;
		}

		public void OnEvent(EventBase @event)
		{
			EventDispatcher dis = new EventDispatcher(@event);
			dis.Dispatch<MouseScrolledEvent>(OnMouseScrolled);
			dis.Dispatch<WindowResizeEvent>(OnWindowResized);
		}

		public void Update(float deltaSeconds)
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			InputManager inputManager = InputManager.Instance;

			Vector3 dir = Vector3.Zero;
			if (inputManager.GetKeyDown(KeyCodes.A))
			{
				dir -= Vector3.UnitX;
				Camera.Position += dir * CAMERA_MOVE_SPEED * deltaSeconds * zoomLevel;
			}
			else if (inputManager.GetKeyDown(KeyCodes.D))
			{
				dir += Vector3.UnitX;
				Camera.Position += dir * CAMERA_MOVE_SPEED * deltaSeconds * zoomLevel;
			}
			if (inputManager.GetKeyDown(KeyCodes.S))
			{
				dir -= Vector3.UnitY;
				Camera.Position += dir * CAMERA_MOVE_SPEED * deltaSeconds * zoomLevel;
			}
			else if (inputManager.GetKeyDown(KeyCodes.W))
			{
				dir += Vector3.UnitY;
				Camera.Position += dir * CAMERA_MOVE_SPEED * deltaSeconds * zoomLevel;
			}

			if (_enableRotation)
			{
				float rot = 0f;
				if (inputManager.GetKeyDown(KeyCodes.Q))
				{
					rot += Util.Deg2Rad(CAMERA_ROTATION_SPEED);
					Camera.Rotation += rot * deltaSeconds;
				}
				else if (inputManager.GetKeyDown(KeyCodes.E))
				{
					rot -= Util.Deg2Rad(CAMERA_ROTATION_SPEED);
					Camera.Rotation += rot * deltaSeconds;
				}
			}
		}

		private bool OnMouseScrolled(MouseScrolledEvent e)
		{
			zoomLevel -= e.MouseDelta * 0.25f;
			zoomLevel = Math.Max(zoomLevel, 0.25f);
			Camera.SetProjectionMatrix(-aspectRatio * zoomLevel, aspectRatio * zoomLevel, -zoomLevel, zoomLevel);
			return false;
		}

		private bool OnWindowResized(WindowResizeEvent e)
		{
			var newAR = (float)e.Width / e.Height;
			// Deduplicate resized events
			if (aspectRatio != newAR)
			{
				aspectRatio = newAR;
				Camera.SetProjectionMatrix(-aspectRatio * zoomLevel, aspectRatio * zoomLevel, -zoomLevel, zoomLevel);
				Application.App.Window.Swapchain.Resize(e.Width, e.Height);
			}
			return false;
		}
	}
}
