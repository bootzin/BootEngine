using BootEngine.ECS.Components;
using BootEngine.ECS.Components.Events.Ecs;
using Leopotam.Ecs;

namespace BootEngine.Renderer
{
	public sealed class RenderSystem : IEcsRunSystem
	{
		private readonly EcsFilter<TransformComponent, SpriteComponent> QuadsFilter = default;
		private readonly EcsFilter<TransformComponent, CameraComponent> CameraFilter = default;
		private readonly EcsFilter<EcsWindowResizeEvent> _windowResizeEvents = default;
		public void Run()
		{
			foreach (int camera in CameraFilter)
			{
				ref var cam = ref CameraFilter.Get2(camera);
				if (cam.Camera.Active)
				{
					Renderer2D.Instance.BeginScene(cam.Camera, CameraFilter.Get1(camera).Transform);
					foreach (int quad in QuadsFilter)
					{
						ref var transform = ref QuadsFilter.Get1(quad);
						ref var sprite = ref QuadsFilter.Get2(quad);
						// TODO: Find a way to avoid calling this function every time. It is the main FPS hog
						// Maybe filter quads within camera fustrum
						Renderer2D.Instance.QueueQuad(transform.Translation, transform.Scale, transform.Rotation, sprite.Color, sprite.Texture);
					}
					Renderer2D.Instance.Render();
					Renderer2D.Instance.EndScene();
				}
			}

			foreach (var ev in _windowResizeEvents)
			{
				Renderer2D.Instance.ResizeSwapchain(_windowResizeEvents.Get1(ev).Event);
			}
		}
	}
}
