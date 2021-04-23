using BootEngine.ECS;
using BootEngine.ECS.Components.Events.Ecs;
using BootEngine.Input;
using BootEngine.Serializers;
using BootEngine.Utils;
using ImGuizmoNET;
using Leopotam.Ecs;
using Shoelace.Services;

namespace Shoelace.Systems
{
	public sealed class GuiControlSystem : IEcsRunSystem
	{
		private readonly EcsFilter<EcsKeyEvent> _keyEvents = default;
		private readonly Scene _scene = default;
		private readonly GuiService _guiService = default;

		public void Run()
		{
			foreach (var kev in _keyEvents)
			{
				HandleKeyEvents(_keyEvents.Get1(kev).Event);
				if (_guiService.BlockEvents)
					_keyEvents.GetEntity(kev).Destroy();
			}
		}

		private void HandleKeyEvents(BootEngine.Events.KeyEvent e)
		{
			bool control = InputManager.Instance.GetKeyDown(KeyCodes.ControlLeft) || InputManager.Instance.GetKeyDown(KeyCodes.ControlRight);
			bool shift = InputManager.Instance.GetKeyDown(KeyCodes.ShiftLeft) || InputManager.Instance.GetKeyDown(KeyCodes.ShiftRight);

			switch (e.KeyCode)
			{
				case KeyCodes.N:
					if (control)
						_guiService.NewScene = true;
					break;
				case KeyCodes.O:
					if (control)
						_guiService.ShouldLoadScene = true;
					break;
				case KeyCodes.S:
					if (control)
					{
						if (shift)
						{
							_guiService.ShouldSaveScene = true;
						}
						else
						{
							new YamlSerializer().Serialize($"assets/scenes/{_scene.Title}.boot", _scene);
						}
					}
					break;
				case KeyCodes.Q:
					if (control)
						_scene.CreateEmptyEntity().AddComponent<EcsWindowCloseEvent>();
					else
						_guiService.GizmoType = null;
					break;
				case KeyCodes.W:
					_guiService.GizmoType = OPERATION.TRANSLATE;
					break;
				case KeyCodes.E:
					_guiService.GizmoType = OPERATION.SCALE;
					break;
				case KeyCodes.R:
					_guiService.GizmoType = OPERATION.ROTATE;
					break;
			}
		}
	}
}
