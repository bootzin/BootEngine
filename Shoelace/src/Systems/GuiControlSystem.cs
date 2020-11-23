using BootEngine.ECS;
using BootEngine.ECS.Components.Events.Ecs;
using BootEngine.Events;
using BootEngine.Input;
using BootEngine.Utils;
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

		private void HandleKeyEvents(KeyEvent e)
		{
			bool control = InputManager.Instance.GetKeyDown(KeyCodes.ControlLeft) || InputManager.Instance.GetKeyDown(KeyCodes.ControlRight);
			bool shift = InputManager.Instance.GetKeyDown(KeyCodes.ShiftLeft) || InputManager.Instance.GetKeyDown(KeyCodes.ShiftRight);

			switch (e.KeyCode)
			{
				case KeyCodes.N:
				case KeyCodes.O:
				case KeyCodes.S:
					break;
				case KeyCodes.Q:
					if (control)
						_scene.CreateEntity().AddComponent<EcsWindowCloseEvent>();
					break;
			}
		}
	}
}
