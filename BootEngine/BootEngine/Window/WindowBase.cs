using BootEngine.Events;
using BootEngine.Utils.ProfilingTools;
using Platforms.Windows;
using System;
using System.Runtime.InteropServices;
using Veldrid;
using Veldrid.Sdl2;

namespace BootEngine.Window
{
	public class WindowProps
	{
		public int Width { get; }
		public int Height { get; }
		public string Title { get; }
		public int X { get; }
		public int Y { get; }
		public bool VSync { get; }
		public WindowState WindowInitialState { get; }

		public WindowProps(string title = "Boot Engine", int width = 1280, int height = 760,
			int x = 0, int y = 0, bool vSync = true, WindowState windowState = WindowState.Normal)
		{
			Height = height;
			Width = width;
			Title = title;
			X = x == 0 ? Sdl2Native.SDL_WINDOWPOS_CENTERED : x;
			Y = y == 0 ? Sdl2Native.SDL_WINDOWPOS_CENTERED : y;
			VSync = vSync;
			WindowInitialState = windowState;
		}
	}

	public abstract class WindowBase : IDisposable
	{
		#region Properties
		public Action<EventBase> EventCallback { get; set; }
		public ResourceFactory ResourceFactory { get; set; }
		public GCHandle GcHandle { get; set; }
		protected bool VSync { get { return GraphicsDevice.SyncToVerticalBlank; } set { SetVSync(value); } }
		public ref GraphicsDevice GraphicsDevice => ref graphicsDevice;
		public Swapchain Swapchain => swapchain;
		public Sdl2Window SdlWindow => window;
		public bool Exists => window.Exists;
		public bool Minimized => window.WindowState == WindowState.Minimized;

		protected GraphicsDevice graphicsDevice;
		protected Sdl2Window window;
		protected Swapchain swapchain;

		private bool disposed;
		#endregion

		#region Methods
		public abstract void OnUpdate(bool updateSnapshot = true);

		public virtual void SetVSync(bool enabled)
		{
			graphicsDevice.SyncToVerticalBlank = enabled;
		}

		public virtual bool IsVSync()
		{
			return graphicsDevice.SyncToVerticalBlank;
		}

		public static WindowBase CreateMainWindow(Type windowType, WindowProps props = null, GraphicsBackend backend = GraphicsBackend.Direct3D11)
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(typeof(WindowBase));
#endif
			if (windowType == typeof(WindowsWindow))
				return new WindowsWindow(props ?? new WindowProps(), backend);
			return null;
		}

		public static WindowBase CreateSubWindow(GraphicsDevice gd, Sdl2Window sdlWindow, Type windowType)
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(typeof(WindowBase));
#endif
			if (windowType == typeof(WindowsWindow))
				return new WindowsWindow(gd, sdlWindow);
			return null;
		}

		#region Dispose
		public void Dispose()
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!disposed)
			{
				if (disposing)
				{
					window.Close();
					EventCallback = null;
					swapchain.Framebuffer.Dispose();
					swapchain.Dispose();
					GcHandle.Free();
				}
				disposed = true;
			}
		}
		#endregion
		#endregion
	}
}
