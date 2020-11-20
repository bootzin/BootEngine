using BootEngine.ECS.Components;
using BootEngine.ECS.Components.Events.Ecs;
using BootEngine.Input;
using BootEngine.Utils;
using Leopotam.Ecs;
using Shoelace.src.Services;
using System.Numerics;

namespace Shoelace.src.Systems
{
	public sealed class EditorCameraSystem : IEcsRunSystem
	{
		// auto injected fields
		private readonly EcsFilter<VelocityComponent, CameraComponent> _cameraTranslationFilter = default;
		private readonly EcsFilter<EcsMouseScrolledEvent> _mouseScrollEvents = default;
		private readonly GuiService _guiService = default;
		private const float CAMERA_ROTATION_SPEED = 60f;
		private const float CAMERA_MOVEMENT_SPEED = 2f;

		public void Run()
		{
			foreach (var camera in _cameraTranslationFilter)
			{
				ref var speed = ref _cameraTranslationFilter.Get1(camera);
				if (_guiService.ViewportFocused && _guiService.ViewportHovered)
				{
					var zoomLevel = CalculateZoom(camera);
					float xVeloc = 0;
					if (InputManager.Instance.GetKeyDown(KeyCodes.A))
					{
						xVeloc = -zoomLevel * CAMERA_MOVEMENT_SPEED;
					}
					else if (InputManager.Instance.GetKeyDown(KeyCodes.D))
					{
						xVeloc = zoomLevel * CAMERA_MOVEMENT_SPEED;
					}

					float yVeloc = 0;
					if (InputManager.Instance.GetKeyDown(KeyCodes.S))
					{
						yVeloc = -zoomLevel;
					}
					else if (InputManager.Instance.GetKeyDown(KeyCodes.W))
					{
						yVeloc = zoomLevel;
					}
					speed.Velocity = new Vector3(xVeloc, yVeloc, 0);

					speed.RotationSpeed = Vector3.Zero;
					if (InputManager.Instance.GetKeyDown(KeyCodes.Q))
					{
						speed.RotationSpeed += Vector3.UnitZ * Util.Deg2Rad(CAMERA_ROTATION_SPEED);
					}
					if (InputManager.Instance.GetKeyDown(KeyCodes.E))
					{
						speed.RotationSpeed -= Vector3.UnitZ * Util.Deg2Rad(CAMERA_ROTATION_SPEED);
					}
				}
				else
				{
					speed.Velocity = Vector3.Zero;
					speed.RotationSpeed = Vector3.Zero;
				}
			}
		}

		private float CalculateZoom(int camera)
		{
			var cam = _cameraTranslationFilter.Get2(camera).Camera;
			foreach (var mse in _mouseScrollEvents)
			{
				var ev = _mouseScrollEvents.Get1(mse).Event;
				cam.ZoomLevel = System.MathF.Max(cam.ZoomLevel - (ev.MouseDelta * .25f), .25f);
			}
			return cam.ZoomLevel;
		}
	}
}
