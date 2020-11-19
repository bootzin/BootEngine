using BootEngine.Events;

namespace BootEngine.ECS.Components.Events.Ecs
{
	public struct EcsKeyEvent
	{
		public KeyEvent Event { get; set; }
	}

	public struct EcsWindowResizeEvent
	{
		public WindowResizeEvent Event { get; set; }
	}

	public struct EcsWindowCloseEvent
	{
		public WindowCloseEvent Event { get; set; }
	}

	public struct EcsMouseMovedEvent
	{
		public MouseMovedEvent Event { get; set; }
	}

	public struct EcsMouseScrolledEvent
	{
		public MouseScrolledEvent Event { get; set; }
	}

	public struct EcsMouseButtonEvent
	{
		public MouseButtonEvent Event { get; set; }
	}
}
