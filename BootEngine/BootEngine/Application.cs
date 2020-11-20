using BootEngine.ECS.Services;
using BootEngine.Events;
using BootEngine.Layers;
using BootEngine.Layers.GUI;
using BootEngine.Logging;
using BootEngine.Utils.ProfilingTools;
using BootEngine.Window;
using System;
using System.Diagnostics;
using Veldrid;

namespace BootEngine
{
	public abstract class Application : IDisposable
	{
		#region Properties
		public static Application App { get; private set; }
		public static TimeService TimeService { get; } = new TimeService();
		public WindowBase Window { get; }
		protected LayerStack LayerStack { get; }
		private ImGuiLayer ImGuiLayer { get; }

		private bool disposed;
		private bool shouldDispose;
		#endregion

		protected Application(WindowProps props, Type windowType, GraphicsBackend backend)
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			Logger.Init();
			Logger.Assert(App == null, "App already initialized");
			App = this;
			LayerStack = new LayerStack();
			Window = WindowBase.CreateMainWindow(windowType, props, backend: backend);
			Window.EventCallback = OnEvent;
			ImGuiLayer = new ImGuiLayer();
			LayerStack.PushOverlay(ImGuiLayer);
		}

		protected Application(Type windowType, GraphicsBackend backend)
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			Logger.Init();
			Logger.Assert(App == null, "App already initialized");
			App = this;
			LayerStack = new LayerStack();
			Window = WindowBase.CreateMainWindow(windowType, backend: backend);
			Window.EventCallback = OnEvent;
			ImGuiLayer = new ImGuiLayer();
			LayerStack.PushOverlay(ImGuiLayer);
		}

		protected Application(Type windowType)
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			Logger.Init();
			Logger.Assert(App == null, "App already initialized");
			App = this;
			LayerStack = new LayerStack();
			Window = WindowBase.CreateMainWindow(windowType);
			Window.EventCallback = OnEvent;
			ImGuiLayer = new ImGuiLayer();
			LayerStack.PushOverlay(ImGuiLayer);
		}

		#region Public Methods
		public void Run()
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			long previousFrameTicks = 0;
			Stopwatch sw = new Stopwatch();
			sw.Start();

			while (Window.Exists)
			{
#if DEBUG
				using (Profiler runLoopProfiler = new Profiler("RunLoop"))
				{
#endif
					long currentFrameTicks = sw.ElapsedTicks;
					TimeService.DeltaSeconds = (currentFrameTicks - previousFrameTicks) / (float)Stopwatch.Frequency;

					previousFrameTicks = currentFrameTicks;

					if (!Window.Minimized)
					{
#if DEBUG
						using (Profiler layerUpdateProfiler = new Profiler("LayersUpdate"))
#endif
							for (int i = 0; i < LayerStack.Layers.Count; i++)
								LayerStack.Layers[i].OnUpdate(TimeService.DeltaSeconds);
					}

#if DEBUG
					using (Profiler imguiProfiler = new Profiler("ImGuiFlow"))
					{
#endif
						ImGuiLayer.Begin(TimeService.DeltaSeconds); //Window is updated in here
						for (int i = 0; i < LayerStack.Layers.Count; i++)
							LayerStack.Layers[i].OnGuiRender();
						ImGuiLayer.End();
#if DEBUG
					}
#endif

					if (Window.Exists)
					{
#if DEBUG
						using (Profiler swapBuffersProfiler = new Profiler("SwapBuffers"))
#endif
							Window.GraphicsDevice.SwapBuffers();
					}
#if DEBUG
				}
#endif
				if (shouldDispose)
					Dispose();
			}
		}

		public void OnEvent(EventBase @event)
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			Logger.CoreInfo(@event);
			for (int index = LayerStack.Layers.Count; index > 0;)
			{
				LayerStack.Layers[--index].OnGenericEvent(@event);
				if (@event.Handled)
					break;
			}
		}

		public void Close()
		{
			shouldDispose = true;
		}
		#endregion

		#region IDisposable Methods
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
					Window.GraphicsDevice.WaitForIdle();
					foreach (LayerBase layer in LayerStack.Layers)
					{
						layer.OnDetach();
					}
					Window.Dispose();
					Window.GraphicsDevice.Dispose();
					LayerStack.Layers.Clear();
				}
				disposed = true;
			}
		}
		#endregion
	}
}
