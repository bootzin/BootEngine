using BootEngine.ECS.Components;
using BootEngine.Events;
using BootEngine.Renderer;
using BootEngine.Renderer.Cameras;
using Leopotam.Ecs;
using System;
using System.Runtime.CompilerServices;

namespace BootEngine.ECS
{
	public sealed class Scene : IDisposable
	{
		private readonly EcsWorld World = new EcsWorld();
		private readonly EcsSystems Systems;
		private readonly EcsSystems RuntimeSystems;

		public string Title { get; set; }

		public Scene()
		{
			Title = "Untitled";
			Systems = new EcsSystems(World, "MainEcsSystems");
			RuntimeSystems = new EcsSystems(World, "Runtime Systems");
			Systems
				.Add(new EventSystem(), "Event System")
				.Add(RuntimeSystems, "Runtime Systems");
		}

		public Scene(string title)
		{
			Title = title;
			Systems = new EcsSystems(World, "MainEcsSystems");
			RuntimeSystems = new EcsSystems(World, "Runtime Systems");
			Systems
				.Add(new EventSystem(), "Event System")
				.Add(RuntimeSystems, "Runtime Systems");
		}

		public Entity CreateEmptyEntity() => new Entity(World.NewEntity());

		public Entity CreateEntity(string name = null)
		{
			Logging.Logger.Assert(Systems.GetAllSystems().Count > 0, "A system must be added before creating entities!");
			Entity e = new Entity(World.NewEntity());
			e.AddComponent<TransformComponent>();
			ref var tag = ref e.AddComponent<TagComponent>();
			tag.Tag = string.IsNullOrEmpty(name) ? "Entity" : name;
			return e;
		}

		public Entity CreateEntity(Entity entity, string name = null)
		{
			Logging.Logger.Assert(Systems.GetAllSystems().Count > 0, "A system must be added before creating entities!");
			Entity e = new Entity(entity);
			ref var tag = ref e.GetComponent<TagComponent>();
			tag.Tag = string.IsNullOrEmpty(name) ? "Copy of " + nameof(entity) : name;
			return e;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Update() => Systems.Run();

		public Scene AddSystem(IEcsSystem sys, string name = null)
		{
			Systems.Add(sys, name);
			return this;
		}

		public Scene AddRuntimeSystem(IEcsSystem sys, string name = null)
		{
			RuntimeSystems.Add(sys, name);
			return this;
		}

		public Scene Inject(object obj)
		{
			Systems.Inject(obj);
			return this;
		}

		public void Init(params object[] injects)
		{
			Systems
				.Add(new CameraSystem(), "Camera System")
				.Add(new RenderSystem(), "Rendering System");
			for (int i = 0; i < injects.Length; i++)
			{
				Systems.Inject(injects[i]);
			}
			Systems.Inject(Application.TimeService);
			Systems.Inject(this);
			EnableRuntimeSystems(false);
			Systems.Init();
		}

		public void EnableRuntimeSystems(bool enabled) => Systems.SetRunSystemState(Systems.GetNamedRunSystem("Runtime Systems"), enabled);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public EcsFilter GetFilter(Type filterType) => World.GetFilter(filterType);

		public void Dispose()
		{
			Systems.Destroy();
			World.Destroy();
		}
	}
}
