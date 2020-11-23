using BootEngine.ECS.Components.Events.Ecs;
using BootEngine.Utils.ProfilingTools;
using Leopotam.Ecs;

namespace BootEngine.Events
{
	internal sealed class EventSystem : IEcsRunSystem
	{
		private readonly EcsFilter<EcsGenericEvent> _events = default;
		private readonly EcsFilter<EcsKeyEvent> _keyEvents = default;
		private readonly EcsFilter<EcsWindowResizeEvent> _windowResizeEvents = default;
		private readonly EcsFilter<EcsWindowCloseEvent> _windowCloseEvents = default;
		private readonly EcsFilter<EcsMouseButtonEvent> _mouseButtonEvents = default;
		private readonly EcsFilter<EcsMouseScrolledEvent> _mouseScrolledEvents = default;
		private readonly EcsFilter<EcsMouseMovedEvent> _mouseMovedEvents = default;
		private readonly EcsWorld _world = default;
		public void Run()
		{
#if DEBUG
			using var _ = new Profiler(GetType());
#endif
			foreach (var ev in _windowCloseEvents)
			{
				_windowCloseEvents.GetEntity(ev).Destroy();
				Application.App.Close();
			}

			foreach (var ev in _keyEvents)
				_keyEvents.GetEntity(ev).Destroy();

			foreach (var ev in _windowResizeEvents)
				_windowResizeEvents.GetEntity(ev).Destroy();

			foreach (var ev in _mouseButtonEvents)
				_mouseButtonEvents.GetEntity(ev).Destroy();

			foreach (var ev in _mouseScrolledEvents)
				_mouseScrolledEvents.GetEntity(ev).Destroy();

			foreach (var ev in _mouseMovedEvents)
				_mouseMovedEvents.GetEntity(ev).Destroy();

			foreach (var ev in _events)
			{
				var @event = _events.Get1(ev).Event;
				ProcessEvent(@event);
				_events.GetEntity(ev).Destroy();
			}
		}

		private void ProcessEvent(EventBase e)
		{
			switch (e)
			{
				case KeyEvent ke:
					ref var kev = ref _world.NewEntity().Get<EcsKeyEvent>();
					kev.Event = ke;
					break;
				case WindowResizeEvent we:
					ref var wev = ref _world.NewEntity().Get<EcsWindowResizeEvent>();
					wev.Event = we;
					break;
				case WindowCloseEvent wce:
					ref var wcev = ref _world.NewEntity().Get<EcsWindowCloseEvent>();
					wcev.Event = wce;
					break;
				case MouseButtonEvent mbe:
					ref var mbev = ref _world.NewEntity().Get<EcsMouseButtonEvent>();
					mbev.Event = mbe;
					break;
				case MouseScrolledEvent mse:
					ref var msev = ref _world.NewEntity().Get<EcsMouseScrolledEvent>();
					msev.Event = mse;
					break;
				case MouseMovedEvent mme:
					ref var mmev = ref _world.NewEntity().Get<EcsMouseMovedEvent>();
					mmev.Event = mme;
					break;
			}
		}
	}
}
