using BootEngine.ECS;
using BootEngine.ECS.Components;
using BootEngine.Scripting;

namespace Shoelace
{
	internal sealed class SampleScript : Script
	{
		private float red;
		private float green;
		private float blue;
		private bool redTurn = true;
		private bool greenTurn;

		private const float threshold = .55f;
		private const float colorChange = 0.0005f;

		public SampleScript(Entity entity) : base(entity)
		{
		}

		public override void OnUpdate()
		{
			if (redTurn)
			{
				if (blue > threshold)
				{
					blue -= colorChange;
				}
				else
				{
					if (blue > 0)
					{
						blue -= colorChange;
					}
					red += colorChange;
					if (red > 1)
					{
						redTurn = false;
						greenTurn = true;
					}
				}
			}
			if (greenTurn)
			{
				if (red > threshold)
				{
					red -= colorChange;
				}
				else
				{
					if (red > 0)
					{
						red -= colorChange;
					}
					green += colorChange;
					if (green > 1)
					{
						greenTurn = false;
					}
				}
			}
			if (!redTurn && !greenTurn)
			{
				if (green > threshold)
				{
					green -= colorChange;
				}
				else
				{
					if (green > 0)
					{
						green -= colorChange;
					}
					blue += colorChange;
					if (blue > 1)
					{
						redTurn = true;
					}
				}
			}
			ref var sc = ref Entity.GetComponent<SpriteRendererComponent>();
			sc.Color = new BootEngine.Utils.ColorF(red, green, blue);
		}
	}
}
