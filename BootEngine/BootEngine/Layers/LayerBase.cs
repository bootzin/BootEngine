using BootEngine.Events;

namespace BootEngine.Layers
{
	public abstract class LayerBase
	{
		#region Properties
		protected string DebugName { get; }
		#endregion

		#region Constructor
		protected LayerBase(string debugName = "Layer")
		{
			DebugName = debugName;
		}
		#endregion

		#region Methods
		public virtual void OnAttach() { }
		public virtual void OnDetach() { }
		public virtual void OnUpdate(float deltaSeconds) { }
		public virtual void OnEvent(EventBase @event) { }
		public virtual void OnGuiRender() { }
		#endregion
	}
}
