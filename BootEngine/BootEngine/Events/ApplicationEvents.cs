namespace BootEngine.Events
{
	public class WindowResizeEvent : EventBase
	{
		#region Properties
		public int Width { get; }
		public int Height { get; }
		#endregion

		#region Constructor
		public WindowResizeEvent(int width, int height)
		{
			Width = width;
			Height = height;
		}
		#endregion

		#region EventBase
		public override EventCategory CategoryFlags => EventCategory.Application;

		public override EventType EventType => EventType.WindowResize;
		#endregion

		#region Methods
		public override string ToString()
		{
			return $"WindowResizeEvent: ({Width}, {Height})";
		}
		#endregion
	}

	public class WindowCloseEvent : EventBase
	{
		#region EventBase
		public override EventCategory CategoryFlags => EventCategory.Application;

		public override EventType EventType => EventType.WindowClose;
		#endregion
	}

	public class AppTickEvent : EventBase
	{
		#region EventBase
		public override EventCategory CategoryFlags => EventCategory.Application;

		public override EventType EventType => EventType.AppTick;
		#endregion
	}

	public class AppUpdateEvent : EventBase
	{
		#region EventBase
		public override EventCategory CategoryFlags => EventCategory.Application;

		public override EventType EventType => EventType.AppUpdate;
		#endregion
	}

	public class AppRenderEvent : EventBase
	{
		#region EventBase
		public override EventCategory CategoryFlags => EventCategory.Application;

		public override EventType EventType => EventType.AppRender;
		#endregion
	}
}
