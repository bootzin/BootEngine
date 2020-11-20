using BootEngine.ECS.Components;
using Leopotam.Ecs;

namespace BootEngine.Renderer
{
	public sealed class RenderSystem : IEcsRunSystem
	{
		private readonly EcsFilter<TransformComponent, SpriteComponent> QuadsFilter = default;
		private readonly EcsFilter<TransformComponent, CameraComponent> CameraFilter = default;
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
						// TODO: Maybe filter quads within camera fustrum
						ref var transform = ref QuadsFilter.Get1(quad);
						ref var sprite = ref QuadsFilter.Get2(quad);
						Renderer2D.Instance.QueueQuad(transform.Position, transform.Scale, transform.Rotation, sprite.Color, sprite.Texture);
					}
					Renderer2D.Instance.Render();
					Renderer2D.Instance.EndScene();
				}
			}
		}
	}
}
