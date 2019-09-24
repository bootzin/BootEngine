using BootEngine.Window;
using System;
using Veldrid;

namespace Platforms.Windows
{
    public class WindowsWindow : WindowBase
    {
        #region Properties
        #endregion

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
        public static WindowBase Create(WindowProps props)
        {
            return new WindowsWindow(props);
        }

        public override void OnUpdate()
        {
            window.PumpEvents();
            graphicsDevice.SwapBuffers();
        }
        #endregion

        #region Private
        private void Initialize(WindowProps props)
        {
            VSync = true;

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
