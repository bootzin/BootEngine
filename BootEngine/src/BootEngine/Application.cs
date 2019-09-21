using System;

namespace BootEngine
{
	public partial class Application : IDisposable
	{
		#region Properties
		private bool disposed = false;
		#endregion

		~Application()
		{
			Dispose(false);
		}

		#region Public Methods
		public virtual void Run()
		{
			while (true)
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
