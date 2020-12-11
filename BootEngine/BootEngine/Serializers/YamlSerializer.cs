using BootEngine.ECS;
using BootEngine.ECS.Components;
using BootEngine.Renderer.Cameras;
using Leopotam.Ecs;
using System;
using System.IO;
using System.Numerics;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace BootEngine.Serializers
{
	public sealed class YamlSerializer
	{
		private readonly ISerializer _serializer;

		public YamlSerializer()
		{
			_serializer = new SerializerBuilder()
				.WithNamingConvention(PascalCaseNamingConvention.Instance)
				.WithMaximumRecursion(7)
				.EnsureRoundtrip()
				.JsonCompatible()
				.IncludeNonPublicProperties()
				.Build();
		}

		public void Serialize<T>(string filePath, T obj)
		{
			using TextWriter tx = File.CreateText(filePath);
			var e = new Emitter(tx, new EmitterSettings());
			e.Emit(new StreamStart());
			e.Emit(new DocumentStart());
			Serialize(e, obj);
			e.Emit(new DocumentEnd(true));
			e.Emit(new StreamEnd());
			tx.Flush();
		}

		internal void Serialize<T>(Emitter e, T obj)
		{
			switch (obj)
			{
				case Scene scene:
					SerializeScene(e, scene);
					break;
				case Entity entity:
					SerializeEntity(e, entity);
					break;
				case Camera cam:
					SerializeCamera(e, cam);
					break;
				case Vector3 vec3:
					SerializeVector3(e, vec3);
					break;
				case Vector4 vec4:
					SerializeVector4(e, vec4);
					break;
				default:
					_serializer.Serialize(e, obj);
					break;
			}
		}

		private void SerializeCamera(Emitter e, Camera cam)
		{
			e.Emit(new Scalar("Camera"));
			e.Emit(new MappingStart());
			{
				e.Emit(new Scalar("ProjectionType")); e.Emit(new Scalar(cam.ProjectionType.ToString()));
				e.Emit(new Scalar("PerspectiveFov")); e.Emit(new Scalar(cam.PerspectiveFov.ToString()));
				e.Emit(new Scalar("PerspectiveNear")); e.Emit(new Scalar(cam.PerspectiveNear.ToString()));
				e.Emit(new Scalar("PerspectiveFar")); e.Emit(new Scalar(cam.PerspectiveFar.ToString()));
				e.Emit(new Scalar("OrthoSize")); e.Emit(new Scalar(cam.OrthoSize.ToString()));
				e.Emit(new Scalar("OrthoNear")); e.Emit(new Scalar(cam.OrthoNear.ToString()));
				e.Emit(new Scalar("OrthoFar")); e.Emit(new Scalar(cam.OrthoFar.ToString()));
				e.Emit(new Scalar("ZoomLevel")); e.Emit(new Scalar(cam.ZoomLevel.ToString()));
			}
			e.Emit(new MappingEnd());
		}

		private void SerializeVector3(Emitter e, Vector3 vec3)
		{
			e.Emit(new SequenceStart(null, null, false, SequenceStyle.Flow));
			e.Emit(new Scalar(vec3.X.ToString()));
			e.Emit(new Scalar(vec3.Y.ToString()));
			e.Emit(new Scalar(vec3.Z.ToString()));
			e.Emit(new SequenceEnd());
		}

		private void SerializeVector4(Emitter e, Vector4 vec4)
		{
			e.Emit(new SequenceStart(null, null, false, SequenceStyle.Flow));
			e.Emit(new Scalar(vec4.X.ToString()));
			e.Emit(new Scalar(vec4.Y.ToString()));
			e.Emit(new Scalar(vec4.Z.ToString()));
			e.Emit(new Scalar(vec4.W.ToString()));
			e.Emit(new SequenceEnd());
		}

		private void SerializeScene(Emitter e, Scene scene)
		{
			e.Emit(new MappingStart());
			{
				e.Emit(new Scalar("Scene"));
				e.Emit(new Scalar(scene.Title));

				e.Emit(new Scalar("Entities"));
				e.Emit(new SequenceStart(null, null, false, SequenceStyle.Block));
				{
					var taggedEntities = scene.GetFilter(typeof(EcsFilter<TagComponent>));
					foreach (var entity in taggedEntities)
					{
						Serialize(e, new Entity(taggedEntities.GetEntity(entity)));
					}
				}
				e.Emit(new SequenceEnd());
			}
			e.Emit(new MappingEnd());
		}

		private void SerializeEntity(Emitter e, Entity entity)
		{
			e.Emit(new MappingStart());
			e.Emit(new Scalar("Entity"));
			e.Emit(new Scalar(Guid.NewGuid().ToString()));
			{
				if (entity.HasComponent<TagComponent>())
				{
					e.Emit(new Scalar("TagComponent"));
					e.Emit(new MappingStart());
					{
						e.Emit(new Scalar("Tag"));
						e.Emit(new Scalar(entity.GetComponent<TagComponent>().Tag));
					}
					e.Emit(new MappingEnd());
				}

				if (entity.HasComponent<TransformComponent>())
				{
					e.Emit(new Scalar("TransformComponent"));
					e.Emit(new MappingStart());
					{
						ref var tc = ref entity.GetComponent<TransformComponent>();
						e.Emit(new Scalar("Translation"));
						Serialize(e, tc.Translation);

						e.Emit(new Scalar("Rotation"));
						Serialize(e, tc.Rotation);

						e.Emit(new Scalar("Scale"));
						Serialize(e, tc.Scale);
					}
					e.Emit(new MappingEnd());
				}

				if (entity.HasComponent<VelocityComponent>())
				{
					e.Emit(new Scalar("VelocityComponent"));
					e.Emit(new MappingStart());
					{
						ref var vc = ref entity.GetComponent<VelocityComponent>();
						e.Emit(new Scalar("Velocity"));
						Serialize(e, vc.Velocity);
						e.Emit(new Scalar("RotationSpeed"));
						Serialize(e, vc.RotationSpeed);
					}
					e.Emit(new MappingEnd());
				}

				if (entity.HasComponent<CameraComponent>())
				{
					e.Emit(new Scalar("CameraComponent"));
					e.Emit(new MappingStart());
					{
						var cam = entity.GetComponent<CameraComponent>().Camera;
						Serialize(e, cam);
						e.Emit(new Scalar("Active")); e.Emit(new Scalar(cam.Active.ToString()));
					}
					e.Emit(new MappingEnd());
				}

				if (entity.HasComponent<SpriteComponent>())
				{
					e.Emit(new Scalar("SpriteComponent"));
					e.Emit(new MappingStart());
					{
						ref var sc = ref entity.GetComponent<SpriteComponent>();
						e.Emit(new Scalar("Color"));
						Serialize(e, sc.Color.ToVector4());

						if (sc.Texture != null)
						{
							e.Emit(new Scalar("Texture"));
							e.Emit(new SequenceStart(null, null, false, SequenceStyle.Flow));
							e.Emit(new Scalar(sc.Texture.Name));
							e.Emit(new Scalar(sc.Texture.Usage.ToString()));
							e.Emit(new SequenceEnd());
						}
					}
					e.Emit(new MappingEnd());
				}
			}
			e.Emit(new MappingEnd());
		}
	}
}
