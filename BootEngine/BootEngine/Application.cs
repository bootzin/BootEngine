using BootEngine.Events;
using BootEngine.Layers;
using BootEngine.Layers.GUI;
using BootEngine.Log;
using BootEngine.Window;
using System;
using System.Linq;

namespace BootEngine
{
    public abstract class Application<WindowType> : IDisposable
	{
		#region Properties
		public static Application<WindowType> App { get; private set; }

		public WindowBase Window { get; set; }
        protected LayerStack LayerStack { get; }

		private ImGuiLayer<WindowType> ImGuiLayer { get; }

		private bool disposed;
        #endregion

        protected Application()
        {
            Logger.Init();
			Logger.Assert(App == null, "App already initialized");
			App = this;
            LayerStack = new LayerStack();
            Window = WindowBase.Create<WindowType>();
            Window.EventCallback = OnEvent;
			ImGuiLayer = new ImGuiLayer<WindowType>();
			LayerStack.PushOverlay(ImGuiLayer);
        }

		~Application()
		{
			Dispose(false);
		}

		#region Public Methods
		public void Run()
		{
			while (Window.Exists())
			{
                LayerStack.Layers.ForEach(layer => layer.OnUpdate());

				ImGuiLayer.Begin();
				LayerStack.Layers.ForEach(layer => layer.OnGuiRender());
				ImGuiLayer.End();

				Window.OnUpdate();
			}
		}

        public void OnEvent(EventBase @event)
        {
            Logger.CoreInfo(@event);
            for (int index = LayerStack.Layers.Count; index > 0;)
            {
                LayerStack.Layers[--index].OnEvent(@event);
                if (@event.Handled)
                    break;
            }
        }
		#endregion

		#region IDisposable Methods
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!disposed)
			{
				if (disposing)
				{
					// called via myClass.Dispose(). 
					// OK to use any private object references
                    Window.Dispose();
                    LayerStack.Layers.Clear();
				}
                // Release unmanaged resources.
                // Set large fields to null.                
                disposed = true;
			}
		}
		#endregion
	}
}
