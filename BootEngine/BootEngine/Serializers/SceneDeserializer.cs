using BootEngine.AssetsManager;
using BootEngine.ECS;
using BootEngine.ECS.Components;
using BootEngine.Logging;
using BootEngine.Renderer;
using BootEngine.Renderer.Cameras;
using BootEngine.Utils;
using System;
using System.Collections;
using System.IO;
using static BootEngine.Serializers.SerializationHelpers;
using Veldrid;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System.Collections.Generic;

namespace BootEngine.Serializers
{
	public sealed class SceneDeserializer
	{
		private static ResourceFactory resourceFactory = Application.App.Window.GraphicsDevice.ResourceFactory;
		private List<ICustomDeserializer> deserializers = new List<ICustomDeserializer>();

		public SceneDeserializer WithCustomDeserializer(ICustomDeserializer deserializer)
		{
			deserializers.Add(deserializer);
			return this;
		}

		public Scene Deserialize(string filePath, Scene scene)
		{
			using TextReader tr = File.OpenText(filePath);
			var d = new DeserializerBuilder()
				.WithNamingConvention(PascalCaseNamingConvention.Instance)
				.WithTagMapping("!BlendStateDescription", typeof(BlendStateDescription))
				.WithTagMapping("!DepthStencilStateDescription", typeof(DepthStencilStateDescription))
				.WithTagMapping("!RasterizerStateDescription", typeof(RasterizerStateDescription))
				.WithTagMapping("!FramebufferAttachment", typeof(FramebufferAttachment))
				.Build();

			dynamic data = d.Deserialize(tr);
			var dataDict = (IDictionary)data;
			if (!dataDict.Contains("Scene"))
			{
				Logger.CoreError("Unable to deserialize scene. Scene was not at the top of the file!");
				return null;
			}

			scene.Title = data["Scene"];
			Logger.CoreVerbose("Deserializing scene " + scene.Title);
			if (dataDict.Contains("Entities"))
			{
				foreach (dynamic entity in data["Entities"])
				{
					dataDict = (IDictionary)entity;
					var entt = scene.CreateEmptyEntity();
					Logger.CoreVerbose("Deserializing entity: " + entt);

					if (dataDict.Contains("TagComponent"))
					{
						ref var tc = ref entt.AddComponent<TagComponent>();
						tc.Tag = entity["TagComponent"]["Tag"];
					}

					if (dataDict.Contains("TransformComponent"))
					{
						ref var tc = ref entt.AddComponent<TransformComponent>();
						tc.Translation = AsVec3(entity["TransformComponent"]["Translation"]);
						tc.Rotation = AsVec3(entity["TransformComponent"]["Rotation"]);
						tc.Scale = AsVec3(entity["TransformComponent"]["Scale"]);
					}

					if (dataDict.Contains("SpriteComponent"))
					{
						ref var sc = ref entt.AddComponent<SpriteRendererComponent>();
						sc.Color = AsVec4(entity["SpriteComponent"]["Color"]);

						dynamic spriteData = (IDictionary)entity["SpriteComponent"]["SpriteData"];
						var tex = spriteData["Texture"];
						sc.SpriteData = new RenderData2D(AsUshortArray(spriteData["Indices"]), AsVertex2DArray(spriteData["Vertices"]),
							tex[0] == "WhiteTex" ? Renderer2D.WhiteTexture : LoadTexture2D(tex));

						dynamic material = entity["SpriteComponent"]["Material"];
						IDictionary materialDict = (IDictionary)entity["SpriteComponent"]["Material"];

						sc.Material = new Material(material["ShaderSetName"]);
						sc.Material.Color = AsVec4(material["Color"]);
						if (materialDict.Contains("Albedo"))
						{
							sc.Material.Albedo = LoadTexture2D(material["Albedo"]);
						}
						if (materialDict.Contains("NormalMap"))
						{
							sc.Material.NormalMap = LoadTexture2D(material["NormalMap"]);
						}
						if (materialDict.Contains("HeightMap"))
						{
							sc.Material.HeightMap = LoadTexture2D(material["HeightMap"]);
						}
						if (materialDict.Contains("Occlusion"))
						{
							sc.Material.Occlusion = LoadTexture2D(material["Occlusion"]);
						}
						sc.Material.Tiling = AsVec2(material["Tiling"]);
						sc.Material.Offset = AsVec2(material["Offset"]);
					}

					if (dataDict.Contains("VelocityComponent"))
					{
						ref var vc = ref entt.AddComponent<VelocityComponent>();
						vc.Velocity = AsVec3(entity["VelocityComponent"]["Velocity"]);
						vc.RotationSpeed = AsVec3(entity["VelocityComponent"]["RotationSpeed"]);
					}

					if (dataDict.Contains("CameraComponent"))
					{
						ref var cc = ref entt.AddComponent<CameraComponent>();
						var camProps = entity["CameraComponent"]["Camera"];

						var cam = new Camera(false);
						cam.Active = bool.Parse(entity["CameraComponent"]["Active"]);
						cam.ProjectionType = Enum.Parse<ProjectionType>(camProps["ProjectionType"]);

						cam.PerspectiveFov = float.Parse(camProps["PerspectiveFov"]);
						cam.PerspectiveFar = float.Parse(camProps["PerspectiveFar"]);
						cam.PerspectiveNear = float.Parse(camProps["PerspectiveNear"]);

						cam.OrthoSize = float.Parse(camProps["OrthoSize"]);
						cam.OrthoFar = float.Parse(camProps["OrthoFar"]);
						cam.OrthoNear = float.Parse(camProps["OrthoNear"]);
						cam.ZoomLevel = float.Parse(camProps["ZoomLevel"]);

						cam.BlendState = camProps["BlendState"];
						cam.DepthStencilState = camProps["DepthStencilState"];
						cam.RasterizerState = camProps["RasterizerState"];

						var depth = camProps["RenderTarget"]["DepthTarget"];
						var colors = camProps["RenderTarget"]["ColorTargets"];

						var depthTex = depth["Texture"];
						cam.DepthTarget = resourceFactory.CreateTexture(new TextureDescription(
							uint.Parse(depthTex["Width"]),
							uint.Parse(depthTex["Height"]),
							uint.Parse(depthTex["Depth"]),
							uint.Parse(depthTex["MipLevels"]),
							uint.Parse(depthTex["ArrayLayers"]),
							Enum.Parse<PixelFormat>(depthTex["Format"]),
							Enum.Parse<TextureUsage>(depthTex["Usage"]),
							Enum.Parse<TextureType>(depthTex["Type"]),
							Enum.Parse<TextureSampleCount>(depthTex["SampleCount"])));
						var depthTarget = new FramebufferAttachmentDescription(cam.DepthTarget, uint.Parse(depth["ArrayLayer"]), uint.Parse(depth["MipLevel"]));

						var colorTargets = new FramebufferAttachmentDescription[colors.Count];
						cam.ColorTargets = new Texture[colors.Count];
						int i = 0;
						foreach (var color in colors)
						{
							var colorTex = color["Texture"];
							cam.ColorTargets[i] = resourceFactory.CreateTexture(new TextureDescription(
								uint.Parse(colorTex["Width"]),
								uint.Parse(colorTex["Height"]),
								uint.Parse(colorTex["Depth"]),
								uint.Parse(colorTex["MipLevels"]),
								uint.Parse(colorTex["ArrayLayers"]),
								Enum.Parse<PixelFormat>(colorTex["Format"]),
								Enum.Parse<TextureUsage>(colorTex["Usage"]),
								Enum.Parse<TextureType>(colorTex["Type"]),
								Enum.Parse<TextureSampleCount>(colorTex["SampleCount"])));
							colorTargets[i] = new FramebufferAttachmentDescription(cam.ColorTargets[i], uint.Parse(color["ArrayLayer"]), uint.Parse(color["MipLevel"]));
							i++;
						}

						cam.RenderTarget = resourceFactory.CreateFramebuffer(new FramebufferDescription(depthTarget, colorTargets));

						cc.Camera = cam;
					}
				}
			}
			foreach (var deserializer in deserializers)
			{
				deserializer.Deserialize(scene, (IDictionary)data);
			}
			return scene;
		}

		private static Texture LoadTexture2D(dynamic tex)
		{
			return AssetManager.LoadTexture2D(tex[0], Enum.Parse<BootEngineTextureUsage>(tex[1]));
		}

		private ushort[] AsUshortArray(IList obj)
		{
			ushort[] array = new ushort[obj.Count];
			for (int i = 0; i < obj.Count; i++)
			{
				array[i] = ushort.Parse(obj[i].ToString());
			}
			return array;
		}

		private Vertex2D[] AsVertex2DArray(dynamic obj)
		{
			Vertex2D[] array = new Vertex2D[obj.Count];
			for (int i = 0; i < obj.Count; i++)
			{
				array[i] = new Vertex2D(AsVec3(obj[i]["Position"]), AsVec2(obj[i]["TexCoord"]));
			}
			return array;
		}
	}
}
