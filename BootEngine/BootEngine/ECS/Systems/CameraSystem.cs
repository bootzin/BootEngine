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
				var cam = CameraFilter.Get2(camera).Camera;
				if (cam.Active)
				{
					Renderer2D.Instance.BeginScene(cam, CameraFilter.Get1(camera).Transform);
					foreach (int quad in QuadsFilter)
					{
						// TODO: Maybe filter quads within camera fustrum
						Renderer2D.Instance.QueueQuad(QuadsFilter.Get1(quad).Transform, QuadsFilter.Get2(quad).Color);
					}
					Renderer2D.Instance.Render();
					Renderer2D.Instance.EndScene();
				}
			}
		}
	}
}
