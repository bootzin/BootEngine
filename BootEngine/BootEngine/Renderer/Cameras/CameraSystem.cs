using BootEngine.ECS.Components;
using BootEngine.ECS.Components.Events;
using Leopotam.Ecs;

namespace BootEngine.Renderer.Cameras
{
	public class CameraSystem : IEcsRunSystem
	{
		private readonly EcsFilter<TransformComponent, CameraComponent> CameraFilter = default;
		private readonly EcsFilter<ViewportResizedEvent> viewportResized = default;

		public void Run()
		{
			foreach (int camera in CameraFilter)
			{
				ref var cam = ref CameraFilter.Get2(camera);
				if (!cam.Camera.FixedAspectRatio)
				{
					foreach (var resize in viewportResized)
					{
						ref var entt = ref viewportResized.GetEntity(resize);
						ref var newSize = ref viewportResized.Get1(resize);
						cam.Camera.ResizeViewport(newSize.Width, newSize.Height);
						entt.Destroy();
					}
				}
			}
		}
	}
}
