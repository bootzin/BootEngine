using BootEngine.ECS;
using BootEngine.ECS.Components;
using BootEngine.Logging;
using BootEngine.Serializers;
using Leopotam.Ecs;
using Shoelace.Components;
using System.Collections;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;

namespace Shoelace.Serializers
{
	internal sealed class EditorCameraSerializer : ICustomSerializer
	{
		private readonly Scene activeScene;

		public EditorCameraSerializer(Scene activeScene)
		{
			this.activeScene = activeScene;
		}

		public void Serialize(Emitter e, YamlSerializer caller)
		{
			var filter = (EcsFilter<TransformComponent, CameraComponent, EditorCameraComponent>)activeScene.GetFilter(typeof(EcsFilter<TransformComponent, CameraComponent, EditorCameraComponent>));
			Logger.Assert(filter.GetEntitiesCount() == 1, "Exactly one editor camera is expected in the scene!");
			foreach (var camera in filter)
			{
				ref var camData = ref filter.Get3(camera);
				e.Emit("EditorCamera");
				e.Emit(new MappingStart());
				{
					e.Emit("Distance");
					e.Emit(camData.Distance.ToString());

					e.Emit("FocalPoint");
					caller.Serialize(e, camData.FocalPoint);
				}
				e.Emit(new MappingEnd());
			}
		}
	}

	internal sealed class EditorCameraDeserializer : ICustomDeserializer
	{
		public void Deserialize(Scene scene, IDictionary data)
		{
			var filter = (EcsFilter<TransformComponent, CameraComponent, EditorCameraComponent>)scene.GetFilter(typeof(EcsFilter<TransformComponent, CameraComponent, EditorCameraComponent>));
			Logger.Assert(filter.GetEntitiesCount() == 1, "Exactly one editor camera is expected in the scene!");
			foreach (var camera in filter)
			{
				ref var camData = ref filter.Get3(camera);
				dynamic ddata = (IDictionary)data["EditorCamera"];
				camData.Distance = float.Parse(ddata["Distance"]);
				camData.FocalPoint = SerializationHelpers.AsVec3(ddata["FocalPoint"]);
			}
		}
	}
}
