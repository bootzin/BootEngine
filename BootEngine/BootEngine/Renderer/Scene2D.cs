using BootEngine.Layers.GUI;
using BootEngine.Utils.ProfilingTools;
using System.Collections.Generic;
using Veldrid;

namespace BootEngine.Renderer
{
	public sealed class Scene2D : Scene
	{
		public GraphicsPipelineDescription PipelineDescrition { get; private set; }

		internal DeviceBuffer IndexBuffer { get; set; }
		internal DeviceBuffer VertexBuffer { get; set; }
		internal DeviceBuffer InstancesVertexBuffer { get; set; }
		internal DeviceBuffer CameraBuffer { get; set; }
		internal Dictionary<Texture, InstancingTextureData> DataPerTexture { get; set; } = new Dictionary<Texture, InstancingTextureData>();
		internal ResourceLayout ResourceLayout { get; set; }
		public static Texture WhiteTexture { get; internal set; }
		public List<Renderable2D> RenderableList { get; internal set; } = new List<Renderable2D>();
		public RenderStats Stats { get; } = new RenderStats();
		internal Pipeline ActivePipeline { get; private set; }
		internal Framebuffer ActiveFramebuffer { get; private set; }

		public void SetPipelineDescrition(GraphicsPipelineDescription pd, Framebuffer targetFb, bool clearMainFramebuffer = false)
		{
			ActiveFramebuffer = targetFb;
			PipelineDescrition = pd;
			ActivePipeline = Application.App.Window.GraphicsDevice.ResourceFactory.CreateGraphicsPipeline(pd);
			ImGuiLayer.ShouldClearBuffers = clearMainFramebuffer;
		}

		protected override void Dispose(bool disposing)
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			if (disposing)
			{
				IndexBuffer.Dispose();
				VertexBuffer.Dispose();
				InstancesVertexBuffer.Dispose();
				CameraBuffer.Dispose();
				if (!ActivePipeline.IsDisposed)
					ActivePipeline.Dispose();
				ResourceLayout.Dispose();
				foreach (var kv in DataPerTexture)
				{
					kv.Value.ResourceSet.Dispose();
					kv.Key.Dispose();
				}
			}
		}
	}

	public class InstancingTextureData
	{
		public InstancingTextureData() { }

		public InstancingTextureData(ResourceSet resourceSet, uint count, uint indexStart)
		{
			ResourceSet = resourceSet;
			Count = count;
			IndexStart = indexStart;
		}

		public ResourceSet ResourceSet { get; set; }
		public uint Count { get; set; }
		public uint IndexStart { get; set; }
		public uint LastInstanceIndex => IndexStart + Count;
	}

	public class RenderStats
	{
		public int DrawCalls { get; set; }
	}
}
