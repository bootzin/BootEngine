﻿using BootEngine.Events;
using BootEngine.Layers;
using BootEngine.Layers.GUI;
using BootEngine.Log;
using BootEngine.Window;
using System;
using System.Diagnostics;

namespace BootEngine
{
	public abstract class Application<WindowType> : IDisposable
	{
		#region Properties
		public static Application<WindowType> App { get; private set; }

		public WindowBase Window { get; }
        protected LayerStack LayerStack { get; }

		private ImGuiLayer<WindowType> ImGuiLayer { get; }
		private bool disposed;
        #endregion

        protected Application(Veldrid.GraphicsBackend backend = Veldrid.GraphicsBackend.Direct3D11)
        {
            Logger.Init();
			Logger.Assert(App == null, "App already initialized");
			App = this;
            LayerStack = new LayerStack();
            Window = WindowBase.CreateMainWindow<WindowType>(backend: backend);
            Window.EventCallback = OnEvent;
			ImGuiLayer = new ImGuiLayer<WindowType>();
			LayerStack.PushOverlay(ImGuiLayer);
        }

		~Application()
		{
			Dispose(false);
		}

		#region Public Methods
		public void Run()
		{
			long previousFrameTicks = 0;
			Stopwatch sw = new Stopwatch();
			sw.Start();

			while (Window.Exists)
			{
				long currentFrameTicks = sw.ElapsedTicks;
				float deltaSeconds = (currentFrameTicks - previousFrameTicks) / (float)Stopwatch.Frequency;

				//while (_limitFrameRate && deltaSeconds < _desiredFrameLengthSeconds)
				//{
				//	currentFrameTicks = sw.ElapsedTicks;
				//	deltaSeconds = (currentFrameTicks - previousFrameTicks) / (double)Stopwatch.Frequency;
				//}

				Logger.Info(deltaSeconds);

				previousFrameTicks = currentFrameTicks;

				Window.OnUpdate();
				LayerStack.Layers.ForEach(layer => layer.OnUpdate(deltaSeconds));

				if (Window.Exists)
				{
					ImGuiLayer.Begin();
					LayerStack.Layers.ForEach(layer => layer.OnGuiRender());
					ImGuiLayer.End();
					Window.GraphicsDevice.SwapBuffers();
				}
			}
		}

        public void OnEvent(EventBase @event)
        {
            Logger.CoreInfo(@event);
            for (int index = LayerStack.Layers.Count; index > 0;)
            {
                LayerStack.Layers[--index].OnEvent(@event);
                if (@event.Handled)
                    break;
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
					Window.GraphicsDevice.WaitForIdle();
					foreach (LayerBase layer in LayerStack.Layers)
					{
						layer.OnDetach();
					}
					Window.Dispose();
					Window.GraphicsDevice.Dispose();
					Window.ResourceFactory = null;
                    LayerStack.Layers.Clear();
				}
                disposed = true;
			}
		}
		#endregion
	}
}
