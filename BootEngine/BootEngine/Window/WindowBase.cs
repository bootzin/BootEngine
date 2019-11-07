using BootEngine.Events;
using Platforms.Windows;
using System;
using System.Runtime.InteropServices;
using Veldrid;
using Veldrid.Sdl2;

namespace BootEngine.Window
{
	public sealed class WindowProps
	{
		public uint Width { get; set; }
		public uint Height { get; set; }
		public string Title { get; set; }
		public int X { get; set; }
		public int Y { get; set; }
		public bool VSync { get; set; }
		public WindowState WindowInitialState { get; set; }

		public WindowProps(string title = "Boot Engine", uint width = 1280, uint height = 760,
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
		protected bool VSync { get { return GraphicsDevice.SyncToVerticalBlank; } set { SetVSync(in value); } }
		public ref GraphicsDevice GraphicsDevice => ref graphicsDevice;
		public Swapchain Swapchain => swapchain;
		public Sdl2Window SdlWindow => window;
		public bool Exists => window.Exists;

		protected GraphicsDevice graphicsDevice;
		protected Sdl2Window window;
		protected Swapchain swapchain;

		private bool disposed;
		#endregion

		#region Methods
		public abstract void OnUpdate(bool updateSnapshot = true);


		public virtual void SetVSync(in bool enabled)
		{
			graphicsDevice.SyncToVerticalBlank = enabled;
		}

		public virtual bool IsVSync()
		{
			return graphicsDevice.SyncToVerticalBlank;
		}

		public static WindowBase CreateMainWindow<T>(in WindowProps props = null, in GraphicsBackend backend = GraphicsBackend.Direct3D11)
		{
			if (typeof(T) == typeof(WindowsWindow))
				return new WindowsWindow(props ?? new WindowProps(), backend);
			return null;
		}

		public static WindowBase CreateSubWindow(in GraphicsDevice gd, in Sdl2Window sdlWindow, in Type windowType)
		{
			if (windowType == typeof(WindowsWindow))
				return new WindowsWindow(gd, sdlWindow);
			return null;
		}

		#region Dispose
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
