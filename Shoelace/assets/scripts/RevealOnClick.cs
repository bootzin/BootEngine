using BootEngine;
using BootEngine.ECS;
using BootEngine.ECS.Components;
using BootEngine.Input;
using BootEngine.Renderer;
using BootEngine.Scripting;
using BootEngine.Utils;
using System.Collections.Generic;
using System.Linq;
using Veldrid;

namespace Shoelace.assets.scripts
{
	public class RevealOnClick : Script
	{
		public Texture Tex { get; private set; }
		public bool Revealed { get; private set; }
		public List<RevealOnClick> Entities { get; set; }
		private float timer = 1f;
		private bool matched;

		public RevealOnClick(Entity entity, KeyCodes key) : base(entity)
		{
			Key = key;
		}

		public KeyCodes Key { get; }

		public override void OnEnable()
		{
			Tex = Entity.GetComponent<SpriteRendererComponent>().SpriteData.Texture;
		}

		public override void OnUpdate()
		{
			if (InputManager.Instance.GetKeyDown(Key))
			{
				Revealed = true;
			}

			if (Entities.Any(e => e.Tex == Tex && e.Revealed && e != this))
				matched = true;

			if (Revealed && !matched)
				timer -= Application.TimeService.DeltaSeconds;

			if (timer < 0f)
			{
				timer = 1f;
				Revealed = false;
			}

			ref var sc = ref Entity.GetComponent<SpriteRendererComponent>();
			sc.SpriteData.Texture = Revealed ? Tex : Renderer2D.WhiteTexture;
		}
	}
}
