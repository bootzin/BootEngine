using BootEngine.ECS.Components;
using BootEngine.Renderer;
using Leopotam.Ecs;

namespace BootEngine.ECS.Systems
{
	public class CameraSystem : IEcsRunSystem
	{
		public EcsFilter<TransformComponent, SpriteComponent> QuadsFilter;
		public EcsFilter<TransformComponent, CameraComponent> CameraFilter;

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
						Renderer2D.Instance.QueueQuad(transform.Position, transform.Scale, transform.Rotation, QuadsFilter.Get2(quad).Color);
					}
					Renderer2D.Instance.Render();
					Renderer2D.Instance.EndScene();
				}
			}
		}
	}
}
