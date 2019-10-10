﻿using BootEngine.Events;
using BootEngine.Input;
using BootEngine.Window;
using System.Runtime.InteropServices;
using Veldrid;
using Veldrid.Sdl2;

namespace Platforms.Windows
{
	public class WindowsWindow : WindowBase
    {
        #region Constructor
		internal WindowsWindow(GraphicsDevice gd, Sdl2Window sdlWindow)
		{
			InitializeSubWindow(gd, sdlWindow);
		}

        public WindowsWindow(WindowProps props)
        {
            Initialize(props);
        }

        ~WindowsWindow()
        {
            Shutdown();
        }
        #endregion

        #region Methods
        #region Public
        public override void OnUpdate()
        {
            InputManager.Snapshot = window.PumpEvents();
        }
        #endregion

        #region Private
		private void InitializeSubWindow(GraphicsDevice gd, Sdl2Window sdlWindow)
		{
			GcHandle = GCHandle.Alloc(this);
			graphicsDevice = gd;
			ResourceFactory = gd.ResourceFactory;
			VSync = gd.SyncToVerticalBlank;
			window = sdlWindow;
			
			SwapchainSource scSrc = WindowStartup.GetSwapchainSource(sdlWindow);
			SwapchainDescription scDesc = new SwapchainDescription(scSrc, (uint)sdlWindow.Width, (uint)sdlWindow.Height,
				gd.SwapchainFramebuffer.OutputDescription.DepthAttachment?.Format, VSync);
			swapchain = ResourceFactory.CreateSwapchain(scDesc);

			window.Resized += () => swapchain.Resize((uint)window.Width, (uint)window.Height);
		}

        private void Initialize(WindowProps props)
        {
			InputManager.CreateInstance<WindowsInput>();

			GcHandle = GCHandle.Alloc(this);

			VSync = props.VSync;

            GraphicsDeviceOptions options = new GraphicsDeviceOptions()
            {
                Debug = false,
                PreferDepthRangeZeroToOne = true,
                PreferStandardClipSpaceYDirection = true,
                ResourceBindingModel = ResourceBindingModel.Improved,
                SyncToVerticalBlank = VSync,
                HasMainSwapchain = true,
                SwapchainDepthFormat = PixelFormat.R16_UNorm,
                SwapchainSrgbFormat = false
            };
#if DEBUG
            options.Debug = true;
#endif
			WindowStartup.CreateWindowAndGraphicsDevice(props, options, GraphicsBackend.Direct3D11, out window, out graphicsDevice);

            ResourceFactory = graphicsDevice.ResourceFactory;
			swapchain = graphicsDevice.MainSwapchain;

			//Called twice possibly because of 2 events being fired: Resize and SizeChanged (and Maximized)
			//#TODO treat it here or hope for Veldrid to be updated with this (Maybe clone and alter source code myself)
			window.Resized += () =>
			{
				graphicsDevice.ResizeMainWindow((uint)window.Width, (uint)window.Height);
				EventCallback(new WindowResizeEvent((uint)window.Width, (uint)window.Height));
			};

            window.Closed += () => EventCallback(new WindowCloseEvent());

            window.KeyUp += (keyEvent) => EventCallback(new KeyReleasedEvent((int)keyEvent.Key));
            window.KeyDown += (keyEvent) => EventCallback(new KeyPressedEvent((int)keyEvent.Key, 1));

            window.MouseDown += (mouseEvent) => EventCallback(new MouseButtonPressedEvent((int)mouseEvent.MouseButton));
            window.MouseUp += (mouseEvent) => EventCallback(new MouseButtonReleasedEvent((int)mouseEvent.MouseButton));
            window.MouseWheel += (mouseEvent) => EventCallback(new MouseScrolledEvent(mouseEvent.WheelDelta));
            window.MouseMove += (mouseEvent) => EventCallback(new MouseMovedEvent(mouseEvent.MousePosition.X, mouseEvent.MousePosition.Y));
        }

        private void Shutdown()
        {
            //Dispose
        }
        #endregion
        #endregion
    }
}
