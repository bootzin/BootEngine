﻿using System;

namespace BootEngine.Events
{
	public class EventDispatcher
	{
		#region Properties
		public EventBase Event { get; }
		#endregion

		#region Constructor
		public EventDispatcher(EventBase eventBase)
		{
			Event = eventBase;
		}
		#endregion

		#region Methods
		public bool Dispatch<T>(Func<T, bool> func) where T : EventBase
		{
			if (Event is T)
			{
				Event.Handled = func(Event as T);
				return true;
			}
			return false;
		}
		#endregion
	}
}
