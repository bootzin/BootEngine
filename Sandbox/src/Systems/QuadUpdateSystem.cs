﻿using BootEngine.AssetsManager;
using BootEngine.ECS;
using BootEngine.ECS.Components;
using BootEngine.Utils;
using Leopotam.Ecs;
using Sandbox.Services;
using System.Numerics;

namespace Sandbox.Systems
{
	public class QuadUpdateSystem : IEcsRunSystem, IEcsInitSystem
	{
		private readonly EcsFilter<TransformComponent, TagComponent, SpriteComponent> _quads = default;
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
					_quads.GetEntity(item).Destroy();
					instanceCount--;
					continue;
				}

				ref var transform = ref _quads.Get1(item);
				ref var sprite = ref _quads.Get3(item);
				sprite.Color = _quadInfoService.SquareColor;

				if (_quads.Get2(item).Tag == "Quad2")
				{
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
				ent.AddComponent(new SpriteComponent()
				{
					Color = _quadInfoService.SquareColor,
					Texture = AssetManager.LoadTexture2D("assets/textures/sampleDog.png", TextureUsage.Sampled)
				});
				ref var transform = ref ent.GetComponent<TransformComponent>();
				transform.Scale *= .1f;
				transform.Translation = new Vector3(-.11f * (instanceCount % 1000), -.11f * (instanceCount / 1000), .5f);
			}
		}
	}
}
