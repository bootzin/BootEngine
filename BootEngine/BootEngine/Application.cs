using BootEngine.Events;
using BootEngine.Log;
using BootEngine.Window;
using System;

namespace BootEngine
{
    public abstract class Application<WindowType> : IDisposable
	{
		#region Properties
        protected WindowBase Window { get; set; }

		private bool disposed;
        #endregion

        protected Application()
        {
            Logger.Init();
            Window = WindowBase.Create<WindowType>();
            Window.EventCallback = OnEvent;
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
                Window.OnUpdate();
			}
		}

        public void OnEvent(EventBase @event)
        {
            Logger.Info(@event);
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
				}
                // Release unmanaged resources.
                // Set large fields to null.                
                disposed = true;
			}
		}
		#endregion
	}
}
