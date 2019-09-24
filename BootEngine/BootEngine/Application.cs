using Platforms.Windows;
using System;

namespace BootEngine
{
    public class Application : IDisposable
	{
		#region Properties
		private bool disposed;
		#endregion

		~Application()
		{
			Dispose(false);
		}

		#region Public Methods
		public virtual void Run()
		{
            var w = new WindowsWindow(new Window.WindowProps());
			while (w.GetNativeWindow().Exists)
			{
				//
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
				}
                // Release unmanaged resources.
                // Set large fields to null.                
                disposed = true;
			}
		}
		#endregion
	}
}
