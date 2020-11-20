using BootEngine.ECS.Components;
using BootEngine.ECS.Components.Events;
using Leopotam.Ecs;

namespace BootEngine.Renderer.Cameras
{
	public sealed class CameraSystem : IEcsRunSystem
	{
		private readonly EcsFilter<CameraComponent> _cameraFilter = default;
		private readonly EcsFilter<ViewportResizedEvent> _viewportResized = default;

		public void Run()
		{
			foreach (int camera in _cameraFilter)
			{
				ref var cam = ref _cameraFilter.Get1(camera);
				foreach (var resize in _viewportResized)
				{
					ref var entt = ref _viewportResized.GetEntity(resize);
					ref var newSize = ref _viewportResized.Get1(resize);
					cam.Camera.ResizeViewport(newSize.Width, newSize.Height);
					entt.Destroy();
				}
			}	
		}
	}
}
