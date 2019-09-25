using BootEngine.Events;
using BootEngine.Window;
using Veldrid;

namespace Platforms.Windows
{
    public class WindowsWindow : WindowBase
    {
        #region Constructor
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
            window.PumpEvents();
            graphicsDevice.SwapBuffers();
        }
        #endregion

        #region Private
        private void Initialize(WindowProps props)
        {
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

            //Called twice possibly because of 2 events being fired: Resize and SizeChanged (and Maximized)
            //#TODO treat it here or hope for Veldrid to be updated with this (Maybe clone and alter source code myself)
            window.Resized += () => EventCallback(new WindowResizeEvent((uint)window.Width, (uint)window.Height));

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
            graphicsDevice.SwapchainFramebuffer.Dispose();
            graphicsDevice.MainSwapchain.Dispose();
            graphicsDevice.Dispose();
        }
        #endregion
        #endregion
    }
}
