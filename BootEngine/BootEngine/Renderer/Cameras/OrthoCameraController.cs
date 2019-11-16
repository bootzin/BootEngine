using BootEngine.Events;
using BootEngine.Input;
using System;
using System.Numerics;
using Utils;

namespace BootEngine.Renderer.Cameras
{
	public class OrthoCameraController : IUpdatable
	{
		private const float CAMERA_MOVE_SPEED = 2f;
		private const float CAMERA_ROTATION_SPEED = 60f;

		private readonly bool enableRotation;
		private float aspectRatio;
		private float zoomLevel = 1f;

		public OrthoCamera Camera { get; }

		public OrthoCameraController(OrthoCamera camera, bool enableRotation = false)
		{
			Camera = camera;
			this.enableRotation = enableRotation;
		}

		public OrthoCameraController(float aspectRatio, bool useReverseDepth = false, bool swapYAxis = false, bool enableRotation = false)
		{
			Camera = new OrthoCamera(-aspectRatio * zoomLevel, aspectRatio * zoomLevel, -zoomLevel, zoomLevel, useReverseDepth, swapYAxis);
			this.enableRotation = enableRotation;
		}

		public void OnEvent(EventBase @event)
		{
			EventDispatcher dis = new EventDispatcher(@event);
			dis.Dispatch<MouseScrolledEvent>(OnMouseScrolled);
			dis.Dispatch<WindowResizeEvent>(OnWindowResized);
		}

		public void Update(float deltaSeconds)
		{
			InputManager inputManager = InputManager.Instance;

			Vector3 dir = Vector3.Zero;
			if (inputManager.GetKeyDown(KeyCodes.A))
			{
				dir -= Vector3.UnitX;
			}
			else if (inputManager.GetKeyDown(KeyCodes.D))
			{
				dir += Vector3.UnitX;
			}
			if (inputManager.GetKeyDown(KeyCodes.S))
			{
				dir -= Vector3.UnitY;
			}
			else if (inputManager.GetKeyDown(KeyCodes.W))
			{
				dir += Vector3.UnitY;
			}

			if (enableRotation)
			{
				float rot = 0f;
				if (inputManager.GetKeyDown(KeyCodes.Q))
				{
					rot += (float)Util.Deg2Rad(CAMERA_ROTATION_SPEED);
				}
				else if (inputManager.GetKeyDown(KeyCodes.E))
				{
					rot -= (float)Util.Deg2Rad(CAMERA_ROTATION_SPEED);
				}
				Camera.Rotation += rot * deltaSeconds;
			}

			Camera.Position += dir * CAMERA_MOVE_SPEED * deltaSeconds * zoomLevel;
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
			aspectRatio = (float)e.Width / e.Height;
			Camera.SetProjectionMatrix(-aspectRatio * zoomLevel, aspectRatio * zoomLevel, -zoomLevel, zoomLevel); 
			return false;
		}
	}
}
