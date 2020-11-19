using BootEngine.ECS;
using BootEngine.ECS.Components.Events.Ecs;
using BootEngine.Events;
using Veldrid;

namespace BootEngine.Layers
{
	public abstract class LayerBase
	{
		#region Properties
		protected string DebugName { get; }
		protected int Width => Application.App.Window.SdlWindow.Width;
		protected int Height => Application.App.Window.SdlWindow.Height;
		protected GraphicsDevice GraphicsDevice => Application.App.Window.GraphicsDevice;
		protected ResourceFactory ResourceFactory => Application.App.Window.GraphicsDevice.ResourceFactory;

		protected Scene ActiveScene { get; } = new Scene();
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
		internal void OnGenericEvent(EventBase @event)
		{
			ActiveScene.CreateEntity(@event.ToString()).AddComponent(new EcsGenericEvent()
			{
				Event = @event
			});
			OnEvent(@event);
		}
		public virtual void OnEvent(EventBase @event) { }
		public virtual void OnGuiRender() { }

		public virtual void Close() => Application.App.Close();
		#endregion
	}
}
