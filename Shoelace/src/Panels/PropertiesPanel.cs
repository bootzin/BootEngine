using BootEngine.ECS.Components;
using ImGuiNET;
using Leopotam.Ecs;
using Shoelace.Services;

namespace Shoelace.Panels
{
	internal sealed class PropertiesPanel : Panel
	{
		private readonly GuiService _guiService = default;

		public override void OnGuiRender()
		{
			ImGui.Begin("Properties");
			if (_guiService.SelectedEntity != default)
			{
				DrawComponents(_guiService.SelectedEntity);
			}
			ImGui.End();
		}

		private void DrawComponents(EcsEntity selectedEntity)
		{
			object[] componentList = null;
			selectedEntity.GetComponentValues(ref componentList);
			var tag = selectedEntity.Get<TagComponent>().Tag;
			if (ImGui.InputText(" ", ref tag, 256u))
			{
				selectedEntity.Get<TagComponent>().Tag = tag;
			}
		}
	}
}
