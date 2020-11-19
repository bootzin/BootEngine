using BootEngine.ECS.Components;
using BootEngine.Events;
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
			Systems.Add(new EventSystem());
		}

		public Entity CreateEntity(string name = null)
		{
			Logging.Logger.Assert(Systems.GetAllSystems().Count > 0, "A system must be added before creating entities!");
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

		public Scene AddSystem(IEcsSystem sys, string name = null)
		{
			Systems.Add(sys, name);
			return this;
		}

		public void Init(params object[] injects)
		{
			for (int i = 0; i < injects.Length; i++)
			{
				Systems.Inject(injects[i]);
			}
			Systems.Init();
		}

		public void Dispose()
		{
			Systems.Destroy();
			World.Destroy();
		}
	}
}
