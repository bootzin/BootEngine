using BootEngine.Events;
using ImGuiNET;
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
		protected bool VSync { get; set; }

        protected GraphicsDevice graphicsDevice;
        protected Sdl2Window window;
		protected Swapchain swapchain;

        private bool disposed;
        #endregion

        #region Methods
        public abstract void OnUpdate(bool updateSnapshot = true);

        public bool Exists() => window.Exists;

        public virtual Sdl2Window GetSdlWindow()
        {
            return window;
        }

		public GraphicsDevice GetGraphicsDevice()
		{
			return graphicsDevice;
		}

		public Swapchain GetSwapchain()
		{
			return swapchain;
		}

        public virtual void SetVSync(bool enabled)
        {
            graphicsDevice.SyncToVerticalBlank = enabled;
            VSync = enabled;
        }

        public virtual bool IsVSync()
        {
            return graphicsDevice.SyncToVerticalBlank;
        }

        public static WindowBase CreateMainWindow<T>(WindowProps props = null)
        {
            if (typeof(T) == typeof(WindowsWindow))
                return new WindowsWindow(props ?? new WindowProps());
            return null;
        }

		public static WindowBase CreateSubWindow(GraphicsDevice gd, Sdl2Window sdlWindow, Type windowType)
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
					ResourceFactory = null;
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
