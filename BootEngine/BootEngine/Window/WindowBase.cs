using BootEngine.Events;
using System;
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
        public WindowState WindowInitialState { get; set; }

        public WindowProps(string title = "Boot Engine", uint width = 1280, uint height = 720,
            int x = 0, int y = 0, WindowState windowState = WindowState.Normal)
        {
            Height = height;
            Width = width;
            Title = title;
            X = x == 0 ? Sdl2Native.SDL_WINDOWPOS_CENTERED : x;
            Y = y == 0 ? Sdl2Native.SDL_WINDOWPOS_CENTERED : y;
            WindowInitialState = windowState;
        }
    }

    public abstract class WindowBase
    {
        public Action<EventBase> EventCallback { get; set; }

        protected ResourceFactory ResourceFactory { get; set; }
        protected bool VSync { get; set; }

        protected GraphicsDevice graphicsDevice;
        protected Sdl2Window window;

        public abstract void OnUpdate();

        public virtual Sdl2Window GetNativeWindow()
        {
            return window;
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
    }
}
