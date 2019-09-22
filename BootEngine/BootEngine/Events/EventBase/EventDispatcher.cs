using System;

namespace BootEngine.Events
{
	public class EventDispatcher<T> where T : EventBase
	{
		#region Properties
		public T Event { get; }
		#endregion

		#region Constructor
		public EventDispatcher(T eventBase)
		{
			Event = eventBase;
		}
		#endregion

		#region Methods
		public bool Dispatch(Func<T, bool> func)
		{
			if (Event.EventType is T)
			{
				Event.Handled = func(Event);
				return true;
			}
			return false;
		}
		#endregion
	}
}
