using BootEngine.ECS;
using BootEngine.ECS.Components;
using BootEngine.Logging;
using BootEngine.Renderer.Cameras;
using BootEngine.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace BootEngine.Serializers
{
	public sealed class SceneDeserializer
	{
		public Scene Deserialize(string filePath, Scene scene)
		{
			using TextReader tr = File.OpenText(filePath);
			var d = new DeserializerBuilder().WithNamingConvention(PascalCaseNamingConvention.Instance).Build();

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
						if (((IDictionary)entity["SpriteComponent"]).Contains("Texture"))
						{
							var tex = entity["SpriteComponent"]["Texture"];
							//sc.Texture = AssetsManager.AssetManager.LoadTexture2D(tex[0], Enum.Parse<TextureUsage>(tex[1]));
						}
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

						var cam = new Camera();
						cam.Active = bool.Parse(entity["CameraComponent"]["Active"]);
						cam.ProjectionType = Enum.Parse<ProjectionType>(camProps["ProjectionType"]);

						cam.PerspectiveFov = float.Parse(camProps["PerspectiveFov"]);
						cam.PerspectiveFar = float.Parse(camProps["PerspectiveFar"]);
						cam.PerspectiveNear = float.Parse(camProps["PerspectiveNear"]);

						cam.OrthoSize = float.Parse(camProps["OrthoSize"]);
						cam.OrthoFar = float.Parse(camProps["OrthoFar"]);
						cam.OrthoNear = float.Parse(camProps["OrthoNear"]);

						cc.Camera = cam;
					}
				}
			}
			return scene;
		}

		private Vector3 AsVec3(IList obj)
		{
			return new Vector3(
				float.Parse(obj[0].ToString()),
				float.Parse(obj[1].ToString()),
				float.Parse(obj[2].ToString()));
		}

		private Vector4 AsVec4(IList obj)
		{
			return new Vector4(
				float.Parse(obj[0].ToString()),
				float.Parse(obj[1].ToString()),
				float.Parse(obj[2].ToString()),
				float.Parse(obj[3].ToString()));
		}
	}
}
