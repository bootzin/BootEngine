﻿using BootEngine.AssetsManager;
using BootEngine.Log;
using BootEngine.Renderer.Cameras;
using BootEngine.Utils.ProfilingTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Utils.Exceptions;
using Veldrid;

namespace BootEngine.Renderer
{
	public sealed class Renderer2D : Renderer<Renderer2D>, IDisposable
	{
		#region Constants
		private const int MAX_QUADS = 1000000;
		#endregion

		#region Properties
		private static Scene2D CurrentScene { get; set; }
		private readonly static GraphicsDevice _gd = Application.App.Window.GraphicsDevice;
		private readonly InstanceVertexInfo[] _instanceList = new InstanceVertexInfo[MAX_QUADS];

		public int InstanceCount { get; private set; }
		#endregion

		#region Constructor
		static Renderer2D()
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(typeof(Renderer2D));
#endif
			CurrentScene = new Scene2D();

			ResourceFactory factory = _gd.ResourceFactory;

			Span<ushort> quadIndices = stackalloc ushort[] { 0, 1, 2, 3 };
			Span<VertexPositionTexture> quadVertices = stackalloc VertexPositionTexture[]
			{
				new VertexPositionTexture(new Vector3(-.5f, .5f, 0f), new Vector2(0.0f, 1.0f)),
				new VertexPositionTexture(new Vector3(.5f, .5f, 0f), new Vector2(1.0f, 1.0f)),
				new VertexPositionTexture(new Vector3(-.5f, -.5f, 0f), new Vector2(0.0f, 0.0f)),
				new VertexPositionTexture(new Vector3(.5f, -.5f, 0f), new Vector2(1.0f, 0.0f))
			};

			BufferDescription vbDescription = new BufferDescription(
				VertexPositionTexture.SizeInBytes * 4,
				BufferUsage.VertexBuffer);
			BufferDescription ibDescription = new BufferDescription(
				4 * sizeof(ushort),
				BufferUsage.IndexBuffer);

			CurrentScene.IndexBuffer = factory.CreateBuffer(ibDescription);
			CurrentScene.VertexBuffer = factory.CreateBuffer(vbDescription);
			_gd.UpdateBuffer(CurrentScene.IndexBuffer, 0, quadIndices.ToArray());
			_gd.UpdateBuffer(CurrentScene.VertexBuffer, 0, quadVertices.ToArray());

			Scene2D.WhiteTexture = factory.CreateTexture(TextureDescription.Texture2D(
				1u, // Width
				1u, // Height
				1,  // Miplevel
				1,  // ArrayLayers
				PixelFormat.R8_G8_B8_A8_UNorm,
				TextureUsage.Sampled));
			uint[] whiteTexture = { 0xffffffff };
			_gd.UpdateTexture(
				Scene2D.WhiteTexture,
				whiteTexture,
				0,  // x
				0,  // y
				0,  // z
				1u, // Width
				1u, // Height
				1,  // Depth
				0,  // Miplevel
				0); // ArrayLayers
			Scene2D.WhiteTexture.Name = "WhiteTex";

			CurrentScene.InstancesVertexBuffer = factory.CreateBuffer(new BufferDescription(InstanceVertexInfo.SizeInBytes * MAX_QUADS, BufferUsage.VertexBuffer));
			CurrentScene.CameraBuffer = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));
			CurrentScene.Shaders = AssetManager.GenerateShadersFromFile("Texture2D.glsl");

			VertexLayoutDescription sharedVertexLayout = new VertexLayoutDescription(
				new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
				new VertexElementDescription("TexCoord", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2));

			VertexLayoutDescription instanceVertexLayout = new VertexLayoutDescription(
				new VertexElementDescription("InstancePosition", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
				new VertexElementDescription("InstanceScale", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
				new VertexElementDescription("InstanceRotation", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float1),
				new VertexElementDescription("InstanceColor", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4));
			instanceVertexLayout.InstanceStepRate = 1;

			CurrentScene.ResourceLayout = factory.CreateResourceLayout(
				new ResourceLayoutDescription(
					new ResourceLayoutElementDescription("ViewProjection", ResourceKind.UniformBuffer, ShaderStages.Vertex),
					new ResourceLayoutElementDescription("Texture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
					new ResourceLayoutElementDescription("Sampler", ResourceKind.Sampler, ShaderStages.Fragment)));

			GraphicsPipelineDescription pipelineDescription = new GraphicsPipelineDescription();
			pipelineDescription.BlendState = BlendStateDescription.SingleAlphaBlend;
			pipelineDescription.DepthStencilState = new DepthStencilStateDescription(
				depthTestEnabled: true,
				depthWriteEnabled: true,
				comparisonKind: ComparisonKind.LessEqual);
			pipelineDescription.RasterizerState = new RasterizerStateDescription(
				cullMode: FaceCullMode.Back,
				fillMode: PolygonFillMode.Solid,
				frontFace: FrontFace.Clockwise,
				depthClipEnabled: true,
				scissorTestEnabled: false);
			pipelineDescription.PrimitiveTopology = PrimitiveTopology.TriangleStrip;
			pipelineDescription.ResourceLayouts = new ResourceLayout[] { CurrentScene.ResourceLayout };
			pipelineDescription.ShaderSet = new ShaderSetDescription(
				vertexLayouts: new VertexLayoutDescription[] { sharedVertexLayout, instanceVertexLayout },
				shaders: CurrentScene.Shaders);
			pipelineDescription.Outputs = _gd.MainSwapchain.Framebuffer.OutputDescription;

			CurrentScene.Pipeline = factory.CreateGraphicsPipeline(pipelineDescription);

			CurrentScene.ResourceSetsPerTexture.Add(Scene2D.WhiteTexture, _gd.ResourceFactory.CreateResourceSet(new ResourceSetDescription(
				CurrentScene.ResourceLayout,
				CurrentScene.CameraBuffer,
				Scene2D.WhiteTexture,
				_gd.LinearSampler)));

			CurrentScene.InstancesPerTexture.Add(Scene2D.WhiteTexture, 0);
		}
		#endregion

		public static void SetCurrentScene(Scene2D scene)
		{
			CurrentScene = scene;
		}

		public void BeginScene(OrthoCamera camera)
		{
			_gd.UpdateBuffer(CurrentScene.CameraBuffer, 0, camera.ViewProjectionMatrix);
		}

		public void EndScene()
		{
			//
		}

		#region Primitives
		public Renderable2D SubmitQuadDraw(Renderable2DParameters parameters, bool flush = false)
			=> SubmitQuadDraw(ref parameters, flush);

		public Renderable2D SubmitQuadDraw(ref Renderable2DParameters parameters, bool flush = false)
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			Renderable2D renderable;
			if (parameters.Texture == null)
				renderable = SetupInstancedQuad(ref parameters);
			else
				renderable = SetupInstancedTextureQuad(ref parameters);

			CurrentScene.RenderableList.Add(renderable);

			if (flush)
				Flush();

			return renderable;
		}

		internal Renderable2D SetupInstancedQuad(ref Renderable2DParameters parameters)
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			CurrentScene.InstancesPerTexture[Scene2D.WhiteTexture]++;
			return new Renderable2D(ref parameters, InstanceCount++);
		}

		internal Renderable2D SetupInstancedTextureQuad(ref Renderable2DParameters parameters)
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			Renderable2D renderable = new Renderable2D(ref parameters, InstanceCount++);

			if (!CurrentScene.ResourceSetsPerTexture.ContainsKey(parameters.Texture))
			{
				CurrentScene.ResourceSetsPerTexture.Add(renderable.Texture, _gd.ResourceFactory.CreateResourceSet(new ResourceSetDescription(
					CurrentScene.ResourceLayout,
					CurrentScene.CameraBuffer,
					renderable.Texture,
					_gd.LinearSampler)));
				CurrentScene.InstancesPerTexture.Add(renderable.Texture, 1);
			}
			else
			{
				CurrentScene.InstancesPerTexture[renderable.Texture]++;
			}

			return renderable;
		}

		public void RemoveQuadDraw(int instanceIndex, bool flush = false)
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			Renderable2D renderable = GetRenderableByInstanceIndex(instanceIndex);
			CurrentScene.InstancesPerTexture[renderable.Texture ?? Scene2D.WhiteTexture]--;
			_instanceList[instanceIndex] = new InstanceVertexInfo();
			InstanceCount--;
			CurrentScene.RenderableList.RemoveAt(instanceIndex);
			if (flush)
				Flush();
		}
		#endregion

		#region Renderable Update
		public void UpdatePosition(string renderableName, Vector3 position) => UpdateTransform(renderableName, position, null, null);
		public void UpdatePosition(int instanceIndex, Vector3 position) => UpdateTransform(instanceIndex, position, null, null);
		public void UpdateSize(string renderableName, Vector2 size) => UpdateTransform(renderableName, null, size, null);
		public void UpdateSize(int instanceIndex, Vector2 size) => UpdateTransform(instanceIndex, null, size, null);
		public void UpdateRotation(string renderableName, float rotation) => UpdateTransform(renderableName, null, null, rotation);
		public void UpdateRotation(int instanceIndex, float rotation) => UpdateTransform(instanceIndex, null, null, rotation);
		public void UpdateTransform(string renderableName, Vector3? position = null, Vector2? size = null, float? rotation = null) => UpdateTransform(GetRenderableByName(renderableName), position, size, rotation);
		public void UpdateTransform(int instanceIndex, Vector3? position = null, Vector2? size = null, float? rotation = null) => UpdateTransform(GetRenderableByInstanceIndex(instanceIndex), position, size, rotation);

		public void UpdateTransform(Renderable2D renderable, Vector3? position = null, Vector2? size = null, float? rotation = null)
		{
			int index = renderable.InstanceIndex;
			if (position.HasValue)
			{
				_instanceList[index].Position = position.Value;
				renderable.Position = position.Value;
			}
			if (size.HasValue)
			{
				_instanceList[index].Scale = size.Value;
				renderable.Size = size.Value;
			}
			if (rotation.HasValue)
			{
				_instanceList[index].Rotation = rotation.Value;
				renderable.Rotation = rotation.Value;
			}
		}

		public void UpdateColor(string renderableName, Vector4 value) => UpdateColor(GetRenderableByName(renderableName), value);

		public void UpdateColor(int instanceIndex, Vector4 value) => UpdateColor(GetRenderableByInstanceIndex(instanceIndex), value);

		public void UpdateColor(Renderable2D renderable, Vector4 value)
		{
			renderable.Color = value;
			_instanceList[renderable.InstanceIndex].Color = value;
		}
		#endregion

		#region Helpers
		public Renderable2D GetRenderableByName(string name)
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			return CurrentScene.RenderableList.Find(r => r.Name == name) as Renderable2D;
		}

		public Renderable2D GetRenderableByInstanceIndex(int instanceIndex)
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			return CurrentScene.RenderableList[instanceIndex] as Renderable2D;
		}

		public void Flush()
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			CurrentScene.RenderableList = CurrentScene.RenderableList.OrderBy(r => ((Renderable2D)r).Texture?.Name).ToList();
			for (int index = 0; index < CurrentScene.RenderableList.Count; index++)
			{
				Renderable2D renderable = (Renderable2D)CurrentScene.RenderableList[index];
				renderable.InstanceIndex = index;
				_instanceList[index].Position = renderable.Position;
				_instanceList[index].Scale = renderable.Size;
				_instanceList[index].Rotation = renderable.Rotation;
				_instanceList[index].Color = renderable.Color;
			}
		}
		#endregion

		#region Renderer
		protected override void BeginRender(CommandList cl)
		{
			cl.Begin();
			cl.SetFramebuffer(_gd.SwapchainFramebuffer);
			cl.SetViewport(0, new Viewport(0, 0, _gd.SwapchainFramebuffer.Width, _gd.SwapchainFramebuffer.Height, 0, 1));
			cl.SetFullViewports();
			cl.ClearColorTarget(0, RgbaFloat.Grey);
			cl.ClearDepthStencil(1f);
			cl.SetVertexBuffer(0, CurrentScene.VertexBuffer);
			cl.SetIndexBuffer(CurrentScene.IndexBuffer, IndexFormat.UInt16);
			cl.SetPipeline(CurrentScene.Pipeline);
		}

		protected override void BatchRender(CommandList cl)
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			cl.UpdateBuffer(CurrentScene.InstancesVertexBuffer, 0, _instanceList);
			cl.SetVertexBuffer(1, CurrentScene.InstancesVertexBuffer);
			uint instanceStart = 0;
			foreach (var entry in CurrentScene.ResourceSetsPerTexture)
			{
				uint instancePerTexCount = CurrentScene.InstancesPerTexture[entry.Key];
				cl.SetGraphicsResourceSet(0, entry.Value);
				cl.DrawIndexed(
					indexCount: 4,
					instanceCount: instancePerTexCount,
					indexStart: 0,
					vertexOffset: 0,
					instanceStart: instanceStart);
				instanceStart += instancePerTexCount;
			}
		}

		protected override void EndRender(CommandList cl)
		{
			cl.End();
			_gd.SubmitCommands(cl);
		}
		#endregion

		public void Dispose()
		{
#if DEBUG
			using Profiler fullProfiler = new Profiler(GetType());
#endif
			CurrentScene.Dispose();
		}

		private readonly struct VertexPositionTexture
		{
			public const uint SizeInBytes = 20;
			public readonly Vector3 Position { get; }
			public readonly Vector2 TexCoord { get; }

			public VertexPositionTexture(Vector3 position, Vector2 texCoord)
			{
				Position = position;
				TexCoord = texCoord;
			}
		}

		private struct InstanceVertexInfo
		{
			public const uint SizeInBytes = 40;

			public Vector3 Position { get; set; }
			public Vector2 Scale { get; set; }
			public float Rotation { get; set; }
			public Vector4 Color { get; set; }
		}
	}

	public ref struct Renderable2DParameters
	{
		public Renderable2DParameters(Vector2? position, Vector2? size, float? rotation, Vector4? color = null, Texture texture = null)
		{
			Name = null;
			Position = new Vector3(position ?? Vector2.Zero, 0f);
			Size = size ?? Vector2.One;
			Rotation = rotation ?? 0f;
			Color = color ?? RgbaFloat.White.ToVector4();
			Texture = texture ?? Scene2D.WhiteTexture;
		}

		public Renderable2DParameters(Vector3? position = null, Vector2? size = null, float? rotation = null, Vector4? color = null, Texture texture = null)
		{
			Name = null;
			Position = position ?? Vector3.Zero;
			Size = size ?? Vector2.One;
			Rotation = rotation ?? 0f;
			Color = color ?? RgbaFloat.White.ToVector4();
			Texture = texture ?? Scene2D.WhiteTexture;
		}

		public Renderable2DParameters(string name, Vector3? position, Vector2? size, float? rotation, Vector4? color = null, Texture texture = null)
		{
			Name = name;
			Position = position ?? Vector3.Zero;
			Size = size ?? Vector2.One;
			Rotation = rotation ?? 0f;
			Color = color ?? RgbaFloat.White.ToVector4();
			Texture = texture ?? Scene2D.WhiteTexture;
		}

		public string Name { get; set; }
		public Vector4 Color { get; set; }
		public Vector3 Position { get; set; }
		public Vector2 Size { get; set; }
		public float Rotation { get; set; }
		public Texture Texture { get; set; }
	}
}
