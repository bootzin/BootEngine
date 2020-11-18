using BootEngine.ECS.Components;
using BootEngine.ECS.Systems;
using Leopotam.Ecs;
using System;

namespace BootEngine.ECS
{
	public sealed class Scene : IDisposable
	{
		internal readonly EcsWorld World = new EcsWorld();
		internal readonly EcsSystems Systems;
		public Scene()
		{
			Systems = new EcsSystems(World, "MainEcsSystems");
		}

		public Entity CreateEntity(string name = null)
		{
			Log.Logger.Assert(Systems.GetAllSystems().Count > 0, "A system must be added before creating entities!");
			Entity e = new Entity(World.NewEntity());
			e.AddComponent<TransformComponent>();
			ref var tag = ref e.AddComponent<TagComponent>();
			tag.Tag = string.IsNullOrEmpty(name) ? "Entity" : name;
			return e;
		}

		public void Update()
		{
			Systems.Run();
		}

		public void AddSystem(IEcsSystem sys, string name = null)
		{
			Systems.Add(sys, name);
		}

		public void Init()
		{
			Systems.Init();
		}

		public void Dispose()
		{
			Systems.Destroy();
			World.Destroy();
		}
	}
}
