using BootEngine;
using BootEngine.AssetsManager;
using BootEngine.ECS;
using BootEngine.ECS.Components;
using BootEngine.Layers.GUI;
using BootEngine.Renderer;
using BootEngine.Renderer.Cameras;
using BootEngine.Utils;
using System;
using System.Collections.Generic;
using Veldrid;

namespace Shoelace
{
	internal static class EditorData
	{
		internal readonly static Dictionary<string, ShaderData> StandardShaders = new Dictionary<string, ShaderData>();
		private readonly static ResourceFactory resourceFactory = Application.App.Window.GraphicsDevice.ResourceFactory;
		private static Texture fbTex, fbDepthTex;

		public static IntPtr SetupEditorCamera(int width, int height, Scene scene)
		{
			var camera = new Camera();
			camera.SetPerspective(MathUtil.Deg2Rad(70), .0001f, 1000f);
			camera.ResizeViewport(width, height);

			var editorCam = scene.CreateEmptyEntity();
			editorCam.AddComponent(new CameraComponent()
			{
				Camera = camera
			});
			editorCam.AddComponent(new TransformComponent() { Translation = new System.Numerics.Vector3(0, 0, 1) });
			camera.BlendState = BlendStateDescription.SingleAlphaBlend;
			camera.DepthStencilState = DepthStencilStateDescription.DepthOnlyLessEqual;
			camera.RasterizerState = RasterizerStateDescription.CullNone;

			fbTex = resourceFactory.CreateTexture(TextureDescription.Texture2D(
				(uint)width, // Width
				(uint)height, // Height
				1,  // Miplevel
				1,  // ArrayLayers
				PixelFormat.R8_G8_B8_A8_UNorm,
				Veldrid.TextureUsage.RenderTarget | Veldrid.TextureUsage.Sampled));

			fbDepthTex = resourceFactory.CreateTexture(TextureDescription.Texture2D(
				(uint)width, // Width
				(uint)height, // Height
				1,  // Miplevel
				1,  // ArrayLayers
				PixelFormat.R16_UNorm,
				Veldrid.TextureUsage.DepthStencil));
			camera.RenderTarget = resourceFactory.CreateFramebuffer(new FramebufferDescription(fbDepthTex, fbTex));
			ImGuiLayer.ShouldClearBuffers = true;
			return ImGuiLayer.GetOrCreateImGuiBinding(resourceFactory, fbTex);
		}

		public static void LoadStandardShaders()
		{
			#region Standard2D
			var standard2DShaders = AssetManager.GenerateShadersFromFile("TexturedInstancing.glsl", "Standard2D");
			var standard2DResourceLayout = resourceFactory.CreateResourceLayout(
									new ResourceLayoutDescription(
										new ResourceLayoutElementDescription("ViewProjection", ResourceKind.UniformBuffer, ShaderStages.Vertex),
										new ResourceLayoutElementDescription("Texture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
										new ResourceLayoutElementDescription("Sampler", ResourceKind.Sampler, ShaderStages.Fragment)));

			Renderer2D.Instance.AddShader("Standard2D", new ShaderData()
			{
				Shaders = standard2DShaders,
				ResourceLayouts = new ResourceLayout[] { standard2DResourceLayout }
			});
			#endregion
		}

		public static void FreeResources()
		{
			fbTex.Dispose();
			fbDepthTex.Dispose();
			foreach (var shaderData in StandardShaders)
			{
				foreach (var shader in shaderData.Value.Shaders)
				{
					shader.Dispose();
				}

				foreach (var resourceLayout in shaderData.Value.ResourceLayouts)
				{
					resourceLayout.Dispose();
				}
			}
		}
	}
}
