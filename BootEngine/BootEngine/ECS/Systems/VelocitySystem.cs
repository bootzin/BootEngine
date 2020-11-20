using BootEngine.ECS.Components;
using BootEngine.ECS.Services;
using Leopotam.Ecs;

namespace BootEngine.ECS.Systems
{
	public class VelocitySystem : IEcsRunSystem
	{
		private readonly EcsFilter<TransformComponent, VelocityComponent> _movables = default;
		private readonly TimeService _time = default;
		public void Run()
		{
			foreach (var movable in _movables)
			{
				ref var transform = ref _movables.Get1(movable);
				ref var velocity = ref _movables.Get2(movable);
				transform.Position += velocity.Velocity * _time.DeltaSeconds;
				transform.Rotation += velocity.RotationSpeed * _time.DeltaSeconds;
			}
		}
	}
}
