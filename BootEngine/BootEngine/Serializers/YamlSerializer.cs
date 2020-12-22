using BootEngine.ECS;
using BootEngine.ECS.Components;
using BootEngine.Renderer.Cameras;
using Leopotam.Ecs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using Veldrid;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace BootEngine.Serializers
{
	public sealed class YamlSerializer
	{
		private readonly IValueSerializer _valueSerializer;
		private readonly List<ICustomSerializer> serializers = new List<ICustomSerializer>();

		public YamlSerializer()
		{
			_valueSerializer = new SerializerBuilder()
				.WithNamingConvention(PascalCaseNamingConvention.Instance)
				.WithMaximumRecursion(7)
				.EnsureRoundtrip()
				.IncludeNonPublicProperties()
				.WithTagMapping("!BlendStateDescription", typeof(BlendStateDescription))
				.WithTagMapping("!DepthStencilStateDescription", typeof(DepthStencilStateDescription))
				.WithTagMapping("!RasterizerStateDescription", typeof(RasterizerStateDescription))
				.WithTagMapping("!FramebufferAttachment", typeof(FramebufferAttachment))
				.BuildValueSerializer();
		}

		public void Serialize<T>(string filePath, T obj)
		{
			using TextWriter tx = File.CreateText(filePath);
			var e = new Emitter(tx, new EmitterSettings());
			e.Emit(new StreamStart());
			e.Emit(new DocumentStart());
			e.Emit(new MappingStart());
			Serialize(e, obj);
			foreach (var serializer in serializers)
			{
				serializer.Serialize(e, this);
			}
			e.Emit(new MappingEnd());
			e.Emit(new DocumentEnd(true));
			e.Emit(new StreamEnd());
			tx.Flush();
		}

		public void Serialize<T>(Emitter e, T obj)
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
				case Texture tex:
					SerializeTexture(e, tex);
					break;
				case FramebufferAttachment fb:
					SerializeFramebufferAttachment(e, fb);
					break;
				case Vector2 vec2:
					SerializeVector2(e, vec2);
					break;
				case Vector3 vec3:
					SerializeVector3(e, vec3);
					break;
				case Vector4 vec4:
					SerializeVector4(e, vec4);
					break;
				default:
					_valueSerializer.SerializeValue(e, obj, obj.GetType());
					break;
			}
		}

		private void SerializeFramebufferAttachment(Emitter e, FramebufferAttachment fb)
		{
			e.Emit("ArrayLayer"); e.Emit(fb.ArrayLayer.ToString());
			e.Emit("MipLevel"); e.Emit(fb.MipLevel.ToString());
			e.Emit("Texture");
			e.Emit(new MappingStart());
			Serialize(e, fb.Target);
			e.Emit(new MappingEnd());
		}

		private void SerializeTexture(Emitter e, Texture tex)
		{
			e.Emit("Width"); e.Emit(tex.Width.ToString());
			e.Emit("Height"); e.Emit(tex.Height.ToString());
			e.Emit("Depth"); e.Emit(tex.Depth.ToString());
			e.Emit("MipLevels"); e.Emit(tex.MipLevels.ToString());
			e.Emit("ArrayLayers"); e.Emit(tex.ArrayLayers.ToString());
			e.Emit("Format"); e.Emit(tex.Format.ToString());
			e.Emit("Usage"); e.Emit(tex.Usage.ToString());
			e.Emit("Type"); e.Emit(tex.Type.ToString());
			e.Emit("SampleCount"); e.Emit(tex.SampleCount.ToString());
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

				e.Emit(new Scalar("BlendState")); Serialize(e, cam.BlendState);
				e.Emit(new Scalar("DepthStencilState")); Serialize(e, cam.DepthStencilState);
				e.Emit(new Scalar("RasterizerState")); Serialize(e, cam.RasterizerState);

				e.Emit(new Scalar("RenderTarget"));
				e.Emit(new MappingStart());
				{
					if (cam.RenderTarget.DepthTarget != null)
					{
						e.Emit("DepthTarget");
						e.Emit(new MappingStart());
						Serialize(e, cam.RenderTarget.DepthTarget);
						e.Emit(new MappingEnd());
					}

					e.Emit("ColorTargets");
					e.Emit(new SequenceStart(null, null, false, SequenceStyle.Block));
					{
						foreach (var colorTarget in cam.RenderTarget.ColorTargets)
						{
							e.Emit(new MappingStart());
							Serialize(e, colorTarget);
							e.Emit(new MappingEnd());
						}
					}
					e.Emit(new SequenceEnd());
				}
				e.Emit(new MappingEnd());
			}
			e.Emit(new MappingEnd());
		}

		private void SerializeVector2(Emitter e, Vector2 vec2)
		{
			e.Emit(new SequenceStart(null, null, false, SequenceStyle.Flow));
			e.Emit(new Scalar(vec2.X.ToString()));
			e.Emit(new Scalar(vec2.Y.ToString()));
			e.Emit(new SequenceEnd());
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

				if (entity.HasComponent<SpriteRendererComponent>())
				{
					e.Emit(new Scalar("SpriteComponent"));
					e.Emit(new MappingStart());
					{
						ref var sc = ref entity.GetComponent<SpriteRendererComponent>();
						e.Emit(new Scalar("Color"));
						Serialize(e, sc.Color.ToVector4());

						e.Emit(new Scalar("SpriteData"));
						e.Emit(new MappingStart());
						{
							e.Emit(new Scalar("Indices"));
							e.Emit(new SequenceStart(null, null, false, SequenceStyle.Flow));
							foreach (var index in sc.SpriteData.Indices)
							{
								e.Emit(new Scalar(index.ToString()));
							}
							e.Emit(new SequenceEnd());

							e.Emit(new Scalar("Vertices"));
							e.Emit(new SequenceStart(null, null, false, SequenceStyle.Block));
							foreach (var vertex in sc.SpriteData.Vertices)
							{
								e.Emit(new MappingStart());
								{
									e.Emit(new Scalar("Position"));
									Serialize(e, vertex.Position);

									e.Emit(new Scalar("TexCoord"));
									Serialize(e, vertex.TexCoord);
								}
								e.Emit(new MappingEnd());
							}
							e.Emit(new SequenceEnd());

							if (sc.SpriteData.Texture != null)
							{
								e.Emit(new Scalar("Texture"));
								e.Emit(new SequenceStart(null, null, false, SequenceStyle.Flow));
								e.Emit(new Scalar(sc.SpriteData.Texture.Name));
								e.Emit(new Scalar(sc.SpriteData.Texture.Usage.ToString()));
								e.Emit(new SequenceEnd());
							}
						}
						e.Emit(new MappingEnd());

						e.Emit(new Scalar("Material"));
						e.Emit(new MappingStart());
						{
							e.Emit(new Scalar("ShaderSetName"));
							e.Emit(new Scalar(sc.Material.ShaderSetName));

							e.Emit(new Scalar("Color"));
							Serialize(e, sc.Material.Color.ToVector4());

							if (sc.Material.Albedo != null)
							{
								e.Emit(new Scalar("Albedo"));
								Serialize(e, sc.Material.Albedo);
							}

							if (sc.Material.NormalMap != null)
							{
								e.Emit(new Scalar("NormalMap"));
								Serialize(e, sc.Material.NormalMap);
							}

							if (sc.Material.HeightMap != null)
							{
								e.Emit(new Scalar("HeightMap"));
								Serialize(e, sc.Material.HeightMap);
							}

							if (sc.Material.Occlusion != null)
							{
								e.Emit(new Scalar("Occlusion"));
								Serialize(e, sc.Material.Occlusion);
							}

							e.Emit(new Scalar("Offset"));
							Serialize(e, sc.Material.Offset);

							e.Emit(new Scalar("Tiling"));
							Serialize(e, sc.Material.Tiling);
						}
						e.Emit(new MappingEnd());
					}
					e.Emit(new MappingEnd());
				}
			}
			e.Emit(new MappingEnd());
		}

		public YamlSerializer WithCustomSerializer(ICustomSerializer customSerializer)
		{
			serializers.Add(customSerializer);
			return this;
		}
	}

	public static class EmitterExtensions
	{
		public static void Emit(this Emitter e, string scalar)
		{
			e.Emit(new Scalar(scalar));
		}
	}
}
