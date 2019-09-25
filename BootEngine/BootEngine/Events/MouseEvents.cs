namespace BootEngine.Events
{
	public class MouseMovedEvent : EventBase
	{
		#region Properties
		public float MouseX { get; }
		public float MouseY { get; }
		#endregion

		#region Constructor
		public MouseMovedEvent(float x, float y)
		{
			MouseX = x;
			MouseY = y;
		}
		#endregion

		#region EventBase
		public override EventCategory CategoryFlags => EventCategory.Mouse | EventCategory.Input;

		public override EventType EventType => EventType.MouseMoved;
		#endregion

		#region Methods
		public override string ToString()
		{
			return "MouseMovedEvent: (" + MouseX + ", " + MouseY + ")";
		}
		#endregion
	}

	public class MouseScrolledEvent : EventBase
	{
		#region Properties
		public float MouseDelta { get; }
		#endregion

		#region Constructor
		public MouseScrolledEvent(float mouseDelta)
		{
            MouseDelta = mouseDelta;
		}
		#endregion

		#region EventBase
		public override EventCategory CategoryFlags => EventCategory.Mouse | EventCategory.Input;

		public override EventType EventType => EventType.MouseScrolled;
		#endregion

		#region Methods
		public override string ToString()
		{
			return "MouseScrolledEvent: (" + MouseDelta + ")";
		}
		#endregion
	}

	public class MouseButtonEvent : EventBase
	{
		#region Properties
		public int MouseButton { get; }
		#endregion

		#region Constructor
		protected MouseButtonEvent(int mButton)
		{
			MouseButton = mButton;
		}
		#endregion

		#region EventBase
		public override EventCategory CategoryFlags => EventCategory.Mouse | EventCategory.Input;
		#endregion
	}

	public class MouseButtonPressedEvent : MouseButtonEvent
	{
		#region Constructor
		public MouseButtonPressedEvent(int mouseButton) : base(mouseButton)
		{
		}
		#endregion

		#region EventBase
		public override EventType EventType => EventType.MouseButtonPressed;
		#endregion

		#region Methods
		public override string ToString()
		{
			return "MouseButtonPressedEvent:" + MouseButton;
		}
		#endregion
	}

	public class MouseButtonReleasedEvent : MouseButtonEvent
	{
		#region Constructor
		public MouseButtonReleasedEvent(int mouseButton) : base(mouseButton)
		{
		}
		#endregion

		#region EventBase
		public override EventType EventType => EventType.MouseButtonReleased;
		#endregion

		#region Methods
		public override string ToString()
		{
			return "MouseButtonReleasedEvent:" + MouseButton;
		}
		#endregion
	}
}
