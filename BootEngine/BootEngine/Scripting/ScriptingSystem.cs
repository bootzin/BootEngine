using BootEngine.ECS.Components;
using Leopotam.Ecs;

namespace BootEngine.Scripting
{
	public sealed class ScriptingSystem : IEcsRunSystem
	{
		private readonly EcsFilter<ScriptingComponent> _scriptedEntities = default;

		public void Run()
		{
			foreach (var sc in _scriptedEntities)
			{
				var script = _scriptedEntities.Get1(sc).Script;
				if (script.Enabled)
					script.OnUpdate();
			}
		}
	}
}
