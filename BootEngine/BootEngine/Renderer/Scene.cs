using BootEngine.ECS;
using BootEngine.ECS.Components;
using Leopotam.Ecs;
using System;

namespace BootEngine.Renderer
{
	public abstract class Scene : IDisposable
	{
		internal readonly EcsWorld World = new EcsWorld();
		protected readonly EcsSystems Systems;
		protected Scene()
		{
			Systems = new EcsSystems(World, "MainEcsSystems");
		}

		public Entity CreateEntity(string name)
		{
			Entity e = new Entity(World.NewEntity());
			e.AddComponent<TransformComponent>();
			ref var tag = ref e.AddComponent<TagComponent>();
			tag.Tag = string.IsNullOrEmpty(name) ? "Entity" : name;
			return e;
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			Systems.Destroy();
			World.Destroy();
		}
	}
}
