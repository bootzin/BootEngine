namespace BootEngine.Events
{
	public abstract class EventBase
	{
		#region DEBUG only
#if DEBUG
		public virtual string GetName()
		{
			return GetType().Name;
		}
#endif
		#endregion

		#region Properties
		public virtual EventType EventType { get { return 0; } }
		public abstract EventCategory CategoryFlags { get; }

		public virtual bool Handled {
			get { return false; }
			set { Handled = value; }
		}
		#endregion

		#region Methods
		public bool IsInCategory(EventCategory category)
		{
			return (CategoryFlags & category) != 0;
		}
		#endregion
	}
}
