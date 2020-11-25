using Leopotam.Ecs;

namespace Shoelace.Panels
{
	internal abstract class Panel : IEcsSystem
	{
		public abstract void OnGuiRender();
	}
}
