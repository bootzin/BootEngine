using BootEngine.ECS;
using BootEngine.ECS.Components;
using BootEngine.Renderer;
using Leopotam.Ecs;
using Sandbox.Services;
using System.Numerics;

namespace Sandbox.Systems
{
	public class QuadUpdateSystem : IEcsRunSystem, IEcsInitSystem
	{
		private readonly EcsFilter<TransformComponent, TagComponent, SpriteRendererComponent> _quads = default;
		private readonly QuadInfoService _quadInfoService = default;
		private readonly Scene _scene = default;
		private int instanceCount = 10;

		public void Init()
		{
			instanceCount = _quadInfoService.QuadCount;
		}

		public void Run()
		{
			foreach (var item in _quads)
			{
				if (item >= _quadInfoService.QuadCount)
				{
					ref var sc = ref _quads.Get3(item);
					sc.SpriteData.Dispose();
					sc.Material.Dispose();
					_quads.GetEntity(item).Destroy();
					instanceCount--;
					continue;
				}

				ref var sprite = ref _quads.Get3(item);
				sprite.Color = _quadInfoService.SquareColor;

				if (_quads.Get2(item).Tag == "Quad2")
				{
					ref var transform = ref _quads.Get1(item);
					transform.Translation = new Vector3(_quadInfoService.SquareColor.X, _quadInfoService.SquareColor.Y, _quadInfoService.SquareColor.Z);
				}
			}

			if (instanceCount < 0)
			{
				instanceCount = 0;
				return;
			}

			for (; instanceCount < _quadInfoService.QuadCount; instanceCount++)
			{
				var ent = _scene.CreateEntity();
				ent.AddComponent(new SpriteRendererComponent()
				{
					Color = _quadInfoService.SquareColor,
					Material = new BootEngine.Renderer.Material("Standard2D"),
					SpriteData = RenderData2D.QuadData
				});
				ref var transform = ref ent.GetComponent<TransformComponent>();
				transform.Scale *= .1f;
				transform.Translation = new Vector3(-.11f * (instanceCount % 1000), -.11f * (instanceCount / 1000), .5f);
			}
		}
	}
}
