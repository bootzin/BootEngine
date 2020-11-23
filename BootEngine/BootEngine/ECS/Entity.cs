using Leopotam.Ecs;
using System.Runtime.CompilerServices;

namespace BootEngine.ECS
{
	public class Entity
	{
		private EcsEntity entityHandle = EcsEntity.Null;

		public Entity(EcsEntity ecsEntity)
		{
			entityHandle = ecsEntity;
		}

		public Entity() { }

		public Entity(Entity other)
		{
			entityHandle = other.entityHandle.Copy();
		}

		public ref T AddComponent<T>() where T : struct
		{
			Logging.Logger.Assert(!HasComponent<T>(), "Entity already has component!");
			return ref entityHandle.Get<T>();
		}

		public Entity AddComponent<T>(T item) where T : struct
		{
			Logging.Logger.Assert(!HasComponent<T>(), "Entity already has component!");
			entityHandle.Replace(in item);
			return this;
		}

		public Entity ReplaceComponent<T>(T item) where T : struct
		{
			entityHandle.Replace(in item);
			return this;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ref T GetOrCreateComponent<T>() where T : struct => ref entityHandle.Get<T>();

		public ref T GetComponent<T>() where T : struct
		{
			Logging.Logger.Assert(HasComponent<T>(), "Entity does not contain component of type: " + typeof(T));
			return ref entityHandle.Get<T>();
		}

		public bool HasComponent<T>() where T : struct
		{
			return entityHandle.Has<T>();
		}

		public void RemoveComponent<T>() where T : struct
		{
			Logging.Logger.Assert(HasComponent<T>(), "Entity does not contain component of type: " + typeof(T));
			entityHandle.Del<T>();
		}
	}
}
