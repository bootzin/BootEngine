namespace BootEngine.Events
{
	public class KeyEvent : EventBase
	{
		#region Constructor
		protected KeyEvent(int keyCode)
		{
			KeyCode = keyCode;
		}
		#endregion

		#region Properties
		public int KeyCode { get; }
		#endregion

		#region EventBase
		public override EventCategory CategoryFlags => EventCategory.Input | EventCategory.Keyboard;
		#endregion
	}

	public class KeyPressedEvent : KeyEvent
	{
		#region Contructor
		public KeyPressedEvent(int keyCode, int repeatCount) : base(keyCode)
		{
			RepeatCount = repeatCount;
		}
		#endregion

		#region Properties
		private int RepeatCount { get; }
		#endregion

		#region EventBase
		public override EventType EventType => EventType.KeyPressed;
		#endregion

		#region Methods
		public override string ToString()
		{
			return $"KeyPressedEvent: {KeyCode} ({RepeatCount} repeats)";
		}

		public static EventType GetEventType => EventType.KeyPressed;
		#endregion
	}

	public class KeyReleasedEvent : KeyEvent
	{
		#region Constructor
		public KeyReleasedEvent(int keyCode) : base(keyCode)
		{
		}
		#endregion

		#region EventBase
		public override EventType EventType => EventType.KeyReleased;
		#endregion

		#region Methods
		public override string ToString()
		{
			return "KeyReleasedEvent: " + KeyCode;
		}

		public static EventType GetEventType => EventType.KeyPressed;
		#endregion
	}
}
