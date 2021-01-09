using BootEngine;
using BootEngine.AssetsManager;
using BootEngine.ECS;
using BootEngine.ECS.Components;
using BootEngine.Layers.GUI;
using BootEngine.Renderer;
using BootEngine.Renderer.Cameras;
using BootEngine.Utils;
using Shoelace.Components;
using System;
using System.Collections.Generic;
using System.IO;
using Veldrid;

namespace Shoelace
{
	internal static class EditorHelper
	{
		internal readonly static Dictionary<string, ShaderData> StandardShaders = new Dictionary<string, ShaderData>();
		private readonly static ResourceFactory resourceFactory = Application.App.Window.GraphicsDevice.ResourceFactory;

		public static IntPtr CreateEditorCamera(int width, int height, Scene scene)
		{
			var camera = new Camera(false);
			camera.SetPerspective(MathUtil.Deg2Rad(45), .1f, 1000f);
			camera.ResizeViewport(width, height);

			var editorCam = scene.CreateEmptyEntity();
			editorCam.AddComponent(new CameraComponent()
			{
				Camera = camera
			});
			editorCam.AddComponent(new TransformComponent() { Translation = new System.Numerics.Vector3(1e-6f, 1e-6f, 1) });
			editorCam.AddComponent(new EditorCameraComponent()
			{
				Distance = 10f
			});
			camera.BlendState = BlendStateDescription.SingleAlphaBlend;
			camera.DepthStencilState = DepthStencilStateDescription.DepthOnlyLessEqual;
			camera.RasterizerState = new RasterizerStateDescription(FaceCullMode.None, PolygonFillMode.Solid, FrontFace.Clockwise, true, false);

			camera.ColorTargets = new Texture[]
			{
				resourceFactory.CreateTexture(TextureDescription.Texture2D(
					(uint)width, // Width
					(uint)height, // Height
					1,  // Miplevel
					1,  // ArrayLayers
					PixelFormat.R8_G8_B8_A8_UNorm,
					TextureUsage.RenderTarget | TextureUsage.Sampled))
			};

			camera.DepthTarget = resourceFactory.CreateTexture(TextureDescription.Texture2D(
				(uint)width, // Width
				(uint)height, // Height
				1,  // Miplevel
				1,  // ArrayLayers
				PixelFormat.R16_UNorm,
				TextureUsage.DepthStencil));

			camera.RenderTarget = resourceFactory.CreateFramebuffer(new FramebufferDescription(camera.DepthTarget, camera.ColorTargets));

			ImGuiLayer.ShouldClearBuffers = true;
			return ImGuiLayer.GetOrCreateImGuiBinding(resourceFactory, camera.ColorTargets[0]);
		}

		public static void LoadStandardShaders()
		{
			#region Standard2D
			var standard2DShaders = AssetManager.GenerateShadersFromFile(GetShaderPath("TexturedInstancing.glsl"), "Standard2D");
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

		#region Templates
		#region Script
		public const string ScriptTemplate =
@"using BootEngine.ECS;
using BootEngine.Scripting;

public class SCRIPT_NAME : Script
{
	public SCRIPT_NAME(Entity entity) : base(entity) { }

	public override void OnUpdate()
	{
		
	}
}";
		#endregion

		#region Shader
		public const string ShaderTemplate =
@"#type vertex
#version 430 core

#type fragment
#version 430 core";
		#endregion
		#endregion

		private static string GetShaderPath(string file)
		{
			return Path.Combine(EditorConfig.AssetDirectory, "shaders", file);
		}

		public static void FreeResources()
		{
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
