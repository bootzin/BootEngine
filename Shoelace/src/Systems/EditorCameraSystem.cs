using BootEngine.ECS.Components;
using BootEngine.ECS.Components.Events.Ecs;
using BootEngine.Input;
using BootEngine.Renderer.Cameras;
using BootEngine.Utils;
using Leopotam.Ecs;
using Shoelace.Components;
using Shoelace.Services;
using System;
using System.Numerics;

namespace Shoelace.Systems
{
	public sealed class EditorCameraSystem : IEcsRunSystem
	{
		// auto injected fields
		private readonly EcsFilter<TransformComponent, CameraComponent, EditorCameraComponent> _editorCameraFilter = default;
		private readonly EcsFilter<EcsMouseScrolledEvent> _mouseScrollEvents = default;
		private readonly GuiService _guiService = default;

		public void Run()
		{
			foreach (var camera in _editorCameraFilter)
			{
				ref var transform = ref _editorCameraFilter.Get1(camera);
				ref var camData = ref _editorCameraFilter.Get3(camera);
				if (_guiService.ViewportHovered || _guiService.ViewportFocused)
				{
					var cam = _editorCameraFilter.Get2(camera).Camera;
					if (InputManager.Instance.GetKeyDown(KeyCodes.AltLeft) && !ImGuiNET.ImGuizmo.IsOver())
					{
						var mousePos = InputManager.Instance.GetMousePosition();
						var delta = (mousePos - camData.LastMousePos) * 0.003f;
						camData.LastMousePos = mousePos;

						if (InputManager.Instance.GetMouseButtonDown(MouseButtonCodes.Middle))
							MousePan(delta, cam, ref transform, ref camData);
						else if (InputManager.Instance.GetMouseButtonDown(MouseButtonCodes.Left))
							MouseRotate(delta, ref transform, ref camData);
						else if (InputManager.Instance.GetMouseButtonDown(MouseButtonCodes.Right))
							MouseZoom(delta.Y, ref camData);
					}

					foreach (var mse in _mouseScrollEvents)
					{
						var ev = _mouseScrollEvents.Get1(mse).Event;
						camData.Distance -= ev.MouseDelta * camData.ZoomSpeed * .1f;
						if (camData.Distance < 1)
							camData.Distance = 1;
					}
				}
				transform.Translation = camData.FocalPoint + (Vector3.UnitZ * camData.Distance); // -GetForwardDirection(ref transform) can be used instead of UnitZ for an FPS-like camera
			}
		}

		private void MousePan(Vector2 delta, Camera cam, ref TransformComponent camTransform, ref EditorCameraComponent camData)
		{
			Vector2 panSpeed = GetPanSpeed(cam);
			Vector3 rightDir = GetRightDirection(ref camTransform);
			int rightSign = rightDir.X > 0 ? -1 : 1;
			Vector3 upDir = GetUpDirection(ref camTransform);
			int upSign = upDir.Y < 0 ? -1 : 1;
			camData.FocalPoint += rightSign * rightDir * delta.X * panSpeed.X * camData.Distance;
			camData.FocalPoint += upSign * upDir * delta.Y * panSpeed.Y * camData.Distance;
		}

		private void MouseRotate(Vector2 delta, ref TransformComponent camTransform, ref EditorCameraComponent camData)
		{
			float yawSign = GetUpDirection(ref camTransform).Y < 0 ? -1 : 1;
			camTransform.Rotation -= new Vector3(delta.Y * camData.RotationSpeed, yawSign * delta.X * camData.RotationSpeed, 0);
		}

		private void MouseZoom(float deltaY, ref EditorCameraComponent camData)
		{
			camData.Distance -= deltaY * camData.ZoomSpeed;
			if (camData.Distance < 1)
				camData.Distance = 1;
		}

		// TODO: Adjust pan speed to my liking
		private Vector2 GetPanSpeed(Camera cam)
		{
			float x = MathF.Min(cam.ViewportWidth / 1000.0f, 2.4f); // max = 2.4f
			float xFactor = (0.0366f * (x * x)) - (0.1778f * x) + 0.3021f;

			float y = MathF.Min(cam.ViewportHeight / 1000.0f, 2.4f); // max = 2.4f
			float yFactor = (0.0366f * (y * y)) - (0.1778f * y) + 0.3021f;
			return new Vector2(xFactor, yFactor);
		}

		private static Vector3 GetRightDirection(ref TransformComponent camTransform) => Vector3.Transform(new Vector3(1, 0, 0), GetOrientation(ref camTransform));
		private static Vector3 GetUpDirection(ref TransformComponent camTransform) => Vector3.Transform(new Vector3(0, 1, 0), GetOrientation(ref camTransform));
		private static Vector3 GetForwardDirection(ref TransformComponent camTransform) => Vector3.Transform(new Vector3(0, 0, -1), GetOrientation(ref camTransform));
		private static Quaternion GetOrientation(ref TransformComponent camTransform) => Quaternion.CreateFromYawPitchRoll(-camTransform.Rotation.Y, -camTransform.Rotation.X, 0);
	}
}
