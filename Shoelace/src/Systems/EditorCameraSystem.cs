using BootEngine.ECS.Components;
using BootEngine.ECS.Components.Events.Ecs;
using BootEngine.ECS.Services;
using BootEngine.Input;
using BootEngine.Renderer.Cameras;
using BootEngine.Utils;
using Leopotam.Ecs;
using Shoelace.Services;
using System.Numerics;

namespace Shoelace.Systems
{
	public sealed class EditorCameraSystem : IEcsRunSystem
	{
		// auto injected fields
		private readonly EcsFilter<TransformComponent, CameraComponent> _cameraTranslationFilter = default;
		private readonly EcsFilter<EcsMouseScrolledEvent> _mouseScrollEvents = default;
		private readonly GuiService _guiService = default;
		private readonly TimeService _time = default;

		private const float CAMERA_ROTATION_SPEED = 60f;
		private const float CAMERA_MOVEMENT_SPEED = 2f;

		public void Run()
		{
			foreach (var camera in _cameraTranslationFilter)
			{
				ref var transform = ref _cameraTranslationFilter.Get1(camera);
				var velocity = Vector3.Zero;
				var rotationSpeed = Vector3.Zero;
				if (_guiService.ViewportFocused && _guiService.ViewportHovered)
				{
					var cam = _cameraTranslationFilter.Get2(camera).Camera;
					var zoomLevel = CalculateZoom(cam);
					float xVeloc = 0;
					if (InputManager.Instance.GetKeyDown(KeyCodes.A))
					{
						xVeloc = -zoomLevel * CAMERA_MOVEMENT_SPEED * cam.OrthoSize;
					}
					else if (InputManager.Instance.GetKeyDown(KeyCodes.D))
					{
						xVeloc = zoomLevel * CAMERA_MOVEMENT_SPEED * cam.OrthoSize;
					}

					float yVeloc = 0;
					if (InputManager.Instance.GetKeyDown(KeyCodes.S))
					{
						yVeloc = -zoomLevel * CAMERA_MOVEMENT_SPEED * cam.OrthoSize;
					}
					else if (InputManager.Instance.GetKeyDown(KeyCodes.W))
					{
						yVeloc = zoomLevel * CAMERA_MOVEMENT_SPEED * cam.OrthoSize;
					}
					velocity = new Vector3(xVeloc, yVeloc, 0);

					rotationSpeed = Vector3.Zero;
					if (InputManager.Instance.GetKeyDown(KeyCodes.Q))
					{
						rotationSpeed += Vector3.UnitZ * MathUtil.Deg2Rad(CAMERA_ROTATION_SPEED);
					}
					if (InputManager.Instance.GetKeyDown(KeyCodes.E))
					{
						rotationSpeed -= Vector3.UnitZ * MathUtil.Deg2Rad(CAMERA_ROTATION_SPEED);
					}
				}
				transform.Translation += velocity * _time.DeltaSeconds;
				transform.Rotation += rotationSpeed * _time.DeltaSeconds;
			}
		}

		private float CalculateZoom(Camera cam)
		{
			foreach (var mse in _mouseScrollEvents)
			{
				var ev = _mouseScrollEvents.Get1(mse).Event;
				cam.ZoomLevel = System.MathF.Max(cam.ZoomLevel - (ev.MouseDelta * .25f), .25f);
			}
			return cam.ZoomLevel;
		}
	}
}
